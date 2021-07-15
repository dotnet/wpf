// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//---------------------------------------------------------------------------
//
// Description:
//   Helper class to the MarkupCompiler class that extends the xaml parser by
//   overriding callbacks appropriately for compile mode.
//
//---------------------------------------------------------------------------

using System;
using MS.Internal.Markup;
using System.Xml;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.ComponentModel;
using MS.Utility;   // for SR

namespace MS.Internal
{
    #region ParserCallbacks

    internal class ParserExtension : XamlParser
    {
        #region Constructor

        internal ParserExtension(MarkupCompiler compiler,
            ParserContext parserContext,
            BamlRecordWriter bamlWriter,
            Stream xamlStream,
            bool pass2) :
            base(parserContext, bamlWriter, xamlStream, false)
        {
            _compiler = compiler;
            _pass2 = pass2;
            Debug.Assert(bamlWriter != null, "Need a BamlRecordWriter for compiling to Baml");
        }

        #endregion Constructor

        #region Overrides

        public override void WriteElementStart(XamlElementStartNode xamlObjectNode)
        {
            string classFullName = null;

            classFullName = _compiler.StartElement(ref _class, _subClass, ref _classModifier, xamlObjectNode.ElementType, string.Empty);

            // If we have a serializer for this element's type, then use that
            // serializer rather than doing default serialization.
            // NOTE:  We currently have faith that the serializer will return when
            //        it is done with the subtree and leave everything in the correct
            //        state.  We may want to limit how much the called serializer can
            //        read so that it is forced to return at the end of the subtree.
            if (xamlObjectNode.SerializerType != null)
            {
                 XamlSerializer serializer;
                 if (xamlObjectNode.SerializerType == typeof(XamlStyleSerializer))
                 {
                     serializer = new XamlStyleSerializer(ParserHooks);
                 }
                 else if (xamlObjectNode.SerializerType == typeof(XamlTemplateSerializer))
                 {
                     serializer = new XamlTemplateSerializer(ParserHooks);
                 }
                 else
                 {
                     serializer = Activator.CreateInstance(
                                         xamlObjectNode.SerializerType,
                                         BindingFlags.Instance | BindingFlags.Public |
                                         BindingFlags.NonPublic | BindingFlags.CreateInstance,
                                         null, null, null) as XamlSerializer;
                 }

                 if (serializer == null)
                 {
                    ThrowException(SRID.ParserNoSerializer,
                                   xamlObjectNode.TypeFullName,
                                   xamlObjectNode.LineNumber,
                                   xamlObjectNode.LinePosition);
                 }
                 else
                 {
                    serializer.ConvertXamlToBaml(TokenReader,
                                       ParserContext, xamlObjectNode,
                                       BamlRecordWriter);
                    _compiler.EndElement(_pass2);
                }
            }
            else if (BamlRecordWriter != null)
            {
                if (classFullName != null)
                {
                    bool isRootPublic = _pass2 ? !_isInternalRoot : _compiler.IsRootPublic;
                    Type rootType = isRootPublic ? xamlObjectNode.ElementType : typeof(ParserExtension);
                    XamlElementStartNode xamlRootObjectNode = new XamlElementStartNode(
                        xamlObjectNode.LineNumber,
                        xamlObjectNode.LinePosition,
                        xamlObjectNode.Depth,
                        _compiler.AssemblyName,
                        classFullName,
                        rootType,
                        xamlObjectNode.SerializerType);

                    base.WriteElementStart(xamlRootObjectNode);
                }
                else
                {
                    base.WriteElementStart(xamlObjectNode);
                }
            }
        }

        private void WriteConnectionId()
        {
            if (!_isSameScope)
            {
                base.WriteConnectionId(++_connectionId);
                _isSameScope = true;
            }
        }

        public override void WriteProperty(XamlPropertyNode xamlPropertyNode)
        {
            MemberInfo memberInfo = xamlPropertyNode.PropInfo;

            if (xamlPropertyNode.AttributeUsage == BamlAttributeUsage.RuntimeName &&
                memberInfo != null)
            {
                // NOTE: Error if local element has runtime Name specified. Change this to
                // a warning in the future when that feature is available.
                if (_compiler.LocalAssembly == memberInfo.ReflectedType.Assembly &&
                    !xamlPropertyNode.IsDefinitionName)
                {
                    ThrowException(SRID.LocalNamePropertyNotAllowed,
                                   memberInfo.ReflectedType.Name,
                                   MarkupCompiler.DefinitionNSPrefix,
                                   xamlPropertyNode.LineNumber,
                                   xamlPropertyNode.LinePosition);
                }

                string attributeValue = xamlPropertyNode.Value;
                if (!_pass2)
                {
                    Debug.Assert(_name == null && _nameField == null, "Name has already been set");
                    _nameField = _compiler.AddNameField(attributeValue, xamlPropertyNode.LineNumber, xamlPropertyNode.LinePosition);
                    _name = attributeValue;
                 }

                 if (_nameField != null || _compiler.IsRootNameScope)
                 {
                     WriteConnectionId();
                 }
            }

            if (memberInfo != null &&
                memberInfo.Name.Equals(STARTUPURI) &&
                KnownTypes.Types[(int)KnownElements.Application].IsAssignableFrom(memberInfo.DeclaringType))
            {
                // if Application.StartupUri property then don't bamlize, but gen code since
                // this is better for perf as Application is not a DO.
                if (!_pass2)
                {
                    _compiler.AddApplicationProperty(memberInfo,
                                                     xamlPropertyNode.Value,
                                                     xamlPropertyNode.LineNumber);
                }
            }
            else
            {
                _compiler.IsBamlNeeded = true;
                base.WriteProperty(xamlPropertyNode);
            }
        }

        public override void WriteUnknownTagStart(XamlUnknownTagStartNode xamlUnknownTagStartNode)
        {
            string localElementFullName = string.Empty;
            NamespaceMapEntry[] namespaceMaps = XamlTypeMapper.GetNamespaceMapEntries(xamlUnknownTagStartNode.XmlNamespace);

            if (namespaceMaps != null && namespaceMaps.Length == 1 && namespaceMaps[0].LocalAssembly)
            {
                string ns = namespaceMaps[0].ClrNamespace;
                if (!string.IsNullOrEmpty(ns))
                {
                    ns += MarkupCompiler.DOT;
                }
                localElementFullName =  ns + xamlUnknownTagStartNode.Value;
            }

            if (localElementFullName.Length > 0 && !_pass2)
            {
                // if local complex property bail out now and handle in 2nd pass when TypInfo is available
                int lastIndex = xamlUnknownTagStartNode.Value.LastIndexOf(MarkupCompiler.DOTCHAR);
                if (-1 == lastIndex)
                {
                    _compiler.StartElement(ref _class,
                                           _subClass,
                                           ref _classModifier,
                                           null,
                                           localElementFullName);
                }
            }
            else
            {
                base.WriteUnknownTagStart(xamlUnknownTagStartNode);
            }
        }

        public override void WriteUnknownTagEnd(XamlUnknownTagEndNode xamlUnknownTagEndNode)
        {
            NamespaceMapEntry[] namespaceMaps = XamlTypeMapper.GetNamespaceMapEntries(xamlUnknownTagEndNode.XmlNamespace);
            bool localTag = namespaceMaps != null && namespaceMaps.Length == 1 && namespaceMaps[0].LocalAssembly;

            if (localTag && !_pass2)
            {
                // if local complex property bail out now and handle in 2nd pass when TypInfo is available
                int lastIndex = xamlUnknownTagEndNode.LocalName.LastIndexOf(MarkupCompiler.DOTCHAR);
                if (-1 == lastIndex)
                {
                    _compiler.EndElement(_pass2);
                }
            }
            else
            {
                base.WriteUnknownTagEnd(xamlUnknownTagEndNode);
            }
        }

        public override void WriteUnknownAttribute(XamlUnknownAttributeNode xamlUnknownAttributeNode)
        {
            bool localAttrib = false;
            string localTagFullName = string.Empty;
            string localAttribName = xamlUnknownAttributeNode.Name;
            NamespaceMapEntry[] namespaceMaps = null;
            MemberInfo miKnownEvent = null;

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

            if (localAttrib && !_pass2)
            {
                // ... and if there are any periods in the attribute name, then ...
                int lastIndex = localAttribName.LastIndexOf(MarkupCompiler.DOTCHAR);

                if (-1 != lastIndex)
                {
                    // ... these might be attached props or events defined by a locally defined component,
                    // but being set on this non-local tag.

                    TypeAndSerializer typeAndSerializer = null;
                    string ownerTagName = localAttribName.Substring(0, lastIndex);

                    if (namespaceMaps != null)
                    {
                        if (namespaceMaps.Length == 1 && namespaceMaps[0].LocalAssembly)
                        {
                            // local prop on a known tag
                            localTagFullName = namespaceMaps[0].ClrNamespace + MarkupCompiler.DOT + ownerTagName;
                        }
                    }
                    else
                    {
                        typeAndSerializer = XamlTypeMapper.GetTypeOnly(xamlUnknownAttributeNode.XmlNamespace,
                                                               ownerTagName);

                        if (typeAndSerializer != null)
                        {
                            // known local attribute on a local tag

                            Type ownerTagType = typeAndSerializer.ObjectType;
                            localTagFullName = ownerTagType.FullName;
                            localAttribName = localAttribName.Substring(lastIndex + 1);

                            // See if attached event first
                            miKnownEvent = ownerTagType.GetMethod(MarkupCompiler.ADD + localAttribName + MarkupCompiler.HANDLER,
                                BindingFlags.Public | BindingFlags.Static  |
                                BindingFlags.FlattenHierarchy);

                            if (miKnownEvent == null)
                            {
                                // Not an attached event, so try for a clr event.
                                miKnownEvent = ownerTagType.GetEvent(localAttribName,
                                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
                            }

                            if (miKnownEvent != null)
                            {
                                if (_events == null)
                                    _events = new ArrayList();

                                _events.Add(new MarkupCompiler.MarkupEventInfo(xamlUnknownAttributeNode.Value,
                                                                               localAttribName,
                                                                               miKnownEvent,
                                                                               xamlUnknownAttributeNode.LineNumber));

                                WriteConnectionId();
                            }
                        }
                        else
                        {
                            namespaceMaps = XamlTypeMapper.GetNamespaceMapEntries(xamlUnknownAttributeNode.XmlNamespace);
                            if (namespaceMaps != null && namespaceMaps.Length == 1 && namespaceMaps[0].LocalAssembly)
                            {
                                // local prop on local tag
                                localTagFullName = namespaceMaps[0].ClrNamespace + MarkupCompiler.DOT + ownerTagName;
                            }
                            else
                            {
                                // unknown prop on local tag -- Error!
                                localTagFullName = string.Empty;
                            }
                        }
                    }

                    if (typeAndSerializer == null)
                    {
                        localAttribName = localAttribName.Substring(lastIndex + 1);
                    }
                }
                // else if it is an unknown non-attached prop on a non-local tag -- instant error!
            }

            if (localTagFullName.Length > 0 && !_pass2)
            {
                if (xamlUnknownAttributeNode.AttributeUsage == BamlAttributeUsage.RuntimeName)
                {
                    string attributeValue = xamlUnknownAttributeNode.Value;

                    Debug.Assert(_name == null && _nameField == null, "Name has already been set");
                    _nameField = _compiler.AddNameField(attributeValue, xamlUnknownAttributeNode.LineNumber, xamlUnknownAttributeNode.LinePosition);
                    _name = attributeValue;

                    if (_nameField != null)
                    {
                        WriteConnectionId();
                    }
                }
                else if (localAttribName.Equals(STARTUPURI) &&
                         _compiler.IsCompilingEntryPointClass)
                {
                    // if Application.StartuoUri property then don't bamlize, but gen code since
                    // this is better for perf as Application is not a DO.
                    PropertyInfo pi = KnownTypes.Types[(int)KnownElements.Application].GetProperty(localAttribName);
                    _compiler.AddApplicationProperty(pi,
                                                     xamlUnknownAttributeNode.Value,
                                                     xamlUnknownAttributeNode.LineNumber);
                    return;
                }
                else if (miKnownEvent == null)
                {
                    // This may or may not be a local event, but there is no way to know in Pass1.
                    // So we prepare for the worst case sceanrio and assume it may be one so that
                    // the Xaml compiler can generate the CreateDelegate code.
                    _compiler.HasLocalEvent = true;
                }
            }
            else
            {
                base.WriteUnknownAttribute(xamlUnknownAttributeNode);
            }

            _compiler.IsBamlNeeded = true;
        }

        public override void WriteElementEnd(XamlElementEndNode xamlEndObjectNode)
        {
            _compiler.EndElement(_pass2);
            base.WriteElementEnd(xamlEndObjectNode);
        }

        /// <summary>
        /// override of GetElementType
        /// </summary>
        public override bool GetElementType(
                XmlReader   xmlReader,
                string      localName,
                string      namespaceUri,
            ref string      assemblyName,
            ref string      typeFullName,
            ref Type        baseType,
            ref Type        serializerType)
        {
            if (!ProcessedRootElement &&
                namespaceUri.Equals(XamlReaderHelper.DefinitionNamespaceURI) &&
                (localName.Equals(XamlReaderHelper.DefinitionCodeTag) ||
                 localName.Equals(XamlReaderHelper.DefinitionXDataTag)))
            {
                MarkupCompiler.ThrowCompilerException(SRID.DefinitionTagNotAllowedAtRoot,
                                                      xmlReader.Prefix,
                                                      localName);
            }

            bool foundElement = base.GetElementType(xmlReader, localName, namespaceUri,
                ref assemblyName, ref typeFullName, ref baseType, ref serializerType);

            if (!ProcessedRootElement)
            {
                int count = xmlReader.AttributeCount;

                // save reader's position, to be restored later
                string attrName = (xmlReader.NodeType == XmlNodeType.Attribute) ? xmlReader.Name : null;

                _isRootTag = true;
                _class = string.Empty;
                _subClass = string.Empty;
                ProcessedRootElement = true;
                XamlTypeMapper.IsProtectedAttributeAllowed = false;
                xmlReader.MoveToFirstAttribute();

                while (--count >= 0)
                {
                    string attribNamespaceURI = xmlReader.LookupNamespace(xmlReader.Prefix);

                    if (attribNamespaceURI != null &&
                        attribNamespaceURI.Equals(XamlReaderHelper.DefinitionNamespaceURI))
                    {
                        MarkupCompiler.DefinitionNSPrefix = xmlReader.Prefix;

                        if (xmlReader.LocalName == CLASS)
                        {
                            _class = xmlReader.Value.Trim();
                            if (_class == string.Empty)
                            {
                                // flag an error for processing later in WriteDefAttribute
                                _class = MarkupCompiler.DOT;
                            }
                            else
                            {
                                // flag this so that the Type Mapper can allow protected
                                // attributes on the markup sub-classed root element only.
                                XamlTypeMapper.IsProtectedAttributeAllowed = true;
                            }
                        }
                        else if (xmlReader.LocalName == XamlReaderHelper.DefinitionTypeArgs)
                        {
                            string genericName = _compiler.GetGenericTypeName(localName, xmlReader.Value);

                            foundElement = base.GetElementType(xmlReader, genericName, namespaceUri,
                                ref assemblyName, ref typeFullName, ref baseType, ref serializerType);

                            if (!foundElement)
                            {
                                NamespaceMapEntry[] namespaceMaps = XamlTypeMapper.GetNamespaceMapEntries(namespaceUri);
                                bool isLocal = namespaceMaps != null && namespaceMaps.Length == 1 && namespaceMaps[0].LocalAssembly;
                                if (!isLocal)
                                {
                                    MarkupCompiler.ThrowCompilerException(SRID.UnknownGenericType,
                                                                          MarkupCompiler.DefinitionNSPrefix,
                                                                          xmlReader.Value,
                                                                          localName);
                                }
                            }
                        }
                        else if (xmlReader.LocalName == SUBCLASS)
                        {
                            _subClass = xmlReader.Value.Trim();
                            if (_subClass == string.Empty)
                            {
                                // flag an error for processing later in WriteDefAttribute
                                _subClass = MarkupCompiler.DOT;
                            }
                            else
                            {
                                _compiler.ValidateFullSubClassName(ref _subClass);
                            }
                        }
                        else if (xmlReader.LocalName == CLASSMODIFIER)
                        {
                            if (!_pass2)
                            {
                                _classModifier = xmlReader.Value.Trim();
                                if (_classModifier == string.Empty)
                                {
                                    // flag an error for processing later in WriteDefAttribute
                                    _classModifier = MarkupCompiler.DOT;
                                }
                            }
                            else
                            {
                                // This direct comparison is ok to do in pass2 as it has already been validated in pass1.
                                // This is to avoid a costly instantiation of the CodeDomProvider in pass2.
                                _isInternalRoot = string.Compare("public", xmlReader.Value.Trim(), StringComparison.OrdinalIgnoreCase) != 0;
                            }
                        }
                    }

                    xmlReader.MoveToNextAttribute();
                }

                if (namespaceUri.Equals(XamlReaderHelper.DefinitionNamespaceURI))
                {
                    xmlReader.MoveToElement();
                }
                else
                {
                    if (attrName == null)
                        xmlReader.MoveToFirstAttribute();
                    else
                        xmlReader.MoveToAttribute(attrName);
                }
            }
            else if (!_compiler.IsBamlNeeded && !_compiler.ProcessingRootContext && _compiler.IsCompilingEntryPointClass && xmlReader.Depth > 0)
            {
                if ((!localName.Equals(MarkupCompiler.CODETAG) &&
                     !localName.Equals(MarkupCompiler.CODETAG + "Extension")) ||
                    !namespaceUri.Equals(XamlReaderHelper.DefinitionNamespaceURI))
                {
                    _compiler.IsBamlNeeded = true;
                }
            }

            return foundElement;
        }

        /// <summary>
        /// override of WriteDynamicEvent
        /// </summary>
        public override void WriteClrEvent(XamlClrEventNode xamlClrEventNode)
        {
            bool isStyleEvent = (xamlClrEventNode.IsStyleSetterEvent || xamlClrEventNode.IsTemplateEvent);
            bool localEvent = _compiler.LocalAssembly == xamlClrEventNode.EventMember.ReflectedType.Assembly;

            if (isStyleEvent)
            {
                if (localEvent)
                {
                    // validate the event handler name per CLS grammar for identifiers
                    _compiler.ValidateEventHandlerName(xamlClrEventNode.EventName, xamlClrEventNode.Value);
                    xamlClrEventNode.LocalAssemblyName = _compiler.AssemblyName;
                    // Pass2 should always be true here as otherwise localEvent will be false, but just being paranoid here.
                    if (_pass2)
                    {
                        XamlTypeMapper.HasInternals = true;
                    }
                }
                else
                {
                    if (!xamlClrEventNode.IsSameScope)
                    {
                        _connectionId++;
                    }

                    xamlClrEventNode.ConnectionId = _connectionId;

                    if (!_pass2)
                    {
                        _compiler.ConnectStyleEvent(xamlClrEventNode);
                    }
                }

                return;
            }

            bool appEvent = KnownTypes.Types[(int)KnownElements.Application].IsAssignableFrom(xamlClrEventNode.EventMember.DeclaringType);

            if (!appEvent)
            {
                if (!_pass2)
                {
                    // validate the event handler name per CLS grammar for identifiers
                    _compiler.ValidateEventHandlerName(xamlClrEventNode.EventName, xamlClrEventNode.Value);

                    if (_events == null)
                        _events = new ArrayList();

                    _events.Add(new MarkupCompiler.MarkupEventInfo(xamlClrEventNode.Value,
                                                                   xamlClrEventNode.EventName,
                                                                   xamlClrEventNode.EventMember,
                                                                   xamlClrEventNode.LineNumber));
                }

                // if not local event ...
                if (!localEvent)
                {
                    WriteConnectionId();
                }
            }
            else if (!_pass2)
            {
                // Since Application is not an Element it doesn't implement IComponentConnector and
                // so needs to add events directly.
                MarkupCompiler.MarkupEventInfo mei = new MarkupCompiler.MarkupEventInfo(xamlClrEventNode.Value,
                                                                                        xamlClrEventNode.EventName,
                                                                                        xamlClrEventNode.EventMember,
                                                                                        xamlClrEventNode.LineNumber);
                _compiler.AddApplicationEvent(mei);
            }

            if (_pass2)
            {
                // if local event, add Baml Attribute Record for local event
                if (localEvent)
                {
                    // validate the event handler name per C# grammar for identifiers
                    _compiler.ValidateEventHandlerName(xamlClrEventNode.EventName, xamlClrEventNode.Value);

                    XamlPropertyNode xamlPropertyNode = new XamlPropertyNode(xamlClrEventNode.LineNumber,
                                                                             xamlClrEventNode.LinePosition,
                                                                             xamlClrEventNode.Depth,
                                                                             xamlClrEventNode.EventMember,
                                                                             _compiler.AssemblyName,
                                                                             xamlClrEventNode.EventMember.ReflectedType.FullName,
                                                                             xamlClrEventNode.EventName,
                                                                             xamlClrEventNode.Value,
                                                                             BamlAttributeUsage.Default,
                                                                             false);

                    XamlTypeMapper.HasInternals = true;

                    base.WriteProperty(xamlPropertyNode);
                }
            }
        }

        /// <summary>
        /// override of WriteEndAttributes
        /// </summary>
        public override void WriteEndAttributes(XamlEndAttributesNode xamlEndAttributesNode)
        {
            if (xamlEndAttributesNode.IsCompact)
                return;

            if (_isRootTag)
            {
                _class = string.Empty;
                _classModifier = string.Empty;
                _subClass = string.Empty;
                XamlTypeMapper.IsProtectedAttributeAllowed = false;
            }

            _isRootTag = false;
            _isSameScope = false;

            if (!_pass2)
            {
                if (_nameField == null)
                {
                    if (_isFieldModifierSet)
                    {
                        ThrowException(SRID.FieldModifierNotAllowed,
                                       MarkupCompiler.DefinitionNSPrefix,
                                       xamlEndAttributesNode.LineNumber,
                                       xamlEndAttributesNode.LinePosition);
                    }
                }
                else if (_fieldModifier != MemberAttributes.Assembly)
                {
                    if (MemberAttributes.Private != _fieldModifier &&
                        MemberAttributes.Assembly != _fieldModifier)
                    {
                        MarkupCompiler.GenerateXmlComments(_nameField, _nameField.Name + " Name Field");
                    }

                    _nameField.Attributes = _fieldModifier;
                    _fieldModifier = MemberAttributes.Assembly;
                }

                _nameField = null;
                _isFieldModifierSet = false;

                _compiler.ConnectNameAndEvents(_name, _events, _connectionId);

                _name = null;

                if (_events != null)
                {
                    _events.Clear();
                    _events = null;
                }
            }
            else
            {
                _compiler.CheckForNestedNameScope();
            }

            // Clear the compiler's generic type argument list 
            // (Strange xaml compilation error MC6025 in unrelated class)
            // The bug arises because the markup compiler's _typeArgsList is set for any tag 
            // that has an x:TypeArguments attribute.   It should be cleared upon reaching the
            // end of the tag's attributes, but this only happens in the non-pass2 case.  If the
            // tag needs pass2 processing, the list is set but not cleared, leading to a mysterious
            // exception in the next tag (<ResourceDictionary>, in the repro).
            _compiler.ClearGenericTypeArgs();

            base.WriteEndAttributes(xamlEndAttributesNode);
        }

        /// <summary>
        /// override of WriteDefTag
        /// </summary>
        public override void WriteDefTag(XamlDefTagNode xamlDefTagNode)
        {
            if (!_pass2)
            {
                _compiler.ProcessDefinitionNamespace(xamlDefTagNode);
            }
            else
            {
                // loop through until after the end of the current definition tag is reached.
                while (!xamlDefTagNode.IsEmptyElement && xamlDefTagNode.XmlReader.NodeType != XmlNodeType.EndElement)
                {
                    xamlDefTagNode.XmlReader.Read();
                }

                xamlDefTagNode.XmlReader.Read();
            }
        }

        /// <summary>
        /// override for handling a new xmlnamespace Uri
        /// </summary>
        public override void WriteNamespacePrefix(XamlXmlnsPropertyNode xamlXmlnsPropertyNode)
        {
            if (!_pass2)
            {
                List<ClrNamespaceAssemblyPair> cnap = XamlTypeMapper.GetClrNamespacePairFromCache(xamlXmlnsPropertyNode.XmlNamespace);
                if (cnap != null)
                {
                    foreach (ClrNamespaceAssemblyPair u in cnap)
                    {
                        _compiler.AddUsing(u.ClrNamespace);
                    }
                }
            }

            base.WriteNamespacePrefix(xamlXmlnsPropertyNode);
        }

        /// <summary>
        /// override for mapping instructions between clr and xml namespaces
        /// </summary>
        public override void WritePIMapping(XamlPIMappingNode xamlPIMappingNode)
        {
            if (!_pass2)
            {
                _compiler.AddUsing(xamlPIMappingNode.ClrNamespace);
            }

            // Local assembly!
            if ((xamlPIMappingNode.AssemblyName == null) || (xamlPIMappingNode.AssemblyName.Length == 0))
            {
                xamlPIMappingNode.AssemblyName = _compiler.AssemblyName;
                bool addMapping = !XamlTypeMapper.PITable.Contains(xamlPIMappingNode.XmlNamespace)
                  || ((ClrNamespaceAssemblyPair)XamlTypeMapper.PITable[xamlPIMappingNode.XmlNamespace]).LocalAssembly
                  || string.IsNullOrEmpty(((ClrNamespaceAssemblyPair)XamlTypeMapper.PITable[xamlPIMappingNode.XmlNamespace]).AssemblyName);
                if (addMapping)
                {
                    ClrNamespaceAssemblyPair namespaceMapping = new ClrNamespaceAssemblyPair(xamlPIMappingNode.ClrNamespace,
                                                                                             xamlPIMappingNode.AssemblyName);

                    namespaceMapping.LocalAssembly = true;
                    XamlTypeMapper.PITable[xamlPIMappingNode.XmlNamespace] = namespaceMapping;
                    XamlTypeMapper.InvalidateMappingCache(xamlPIMappingNode.XmlNamespace);
                    if (!_pass2 && BamlRecordWriter != null)
                    {
                        BamlRecordWriter = null;
                    }
                }
            }

            base.WritePIMapping(xamlPIMappingNode);
        }

        /// <summary>
        /// override of WriteDefAttribute
        /// </summary>
        public override void WriteDefAttribute(XamlDefAttributeNode xamlDefAttributeNode)
        {
            if (xamlDefAttributeNode.AttributeUsage == BamlAttributeUsage.RuntimeName)
            {
                string attributeValue = xamlDefAttributeNode.Value;

                if (!_pass2)
                {
                    Debug.Assert(_name == null && _nameField == null, "Name definition has already been set");
                    _nameField = _compiler.AddNameField(attributeValue, xamlDefAttributeNode.LineNumber, xamlDefAttributeNode.LinePosition);
                    _name = attributeValue;
                }

                if (_nameField != null || _compiler.IsRootNameScope)
                {
                    WriteConnectionId();

                    // x:Name needs to be written out as a BAML record in order
                    // to trigger the RegisterName code path in BamlRecordReader.
                    // This code follows the code in WriteProperty for the RuntimeName property
                    base.WriteDefAttribute(xamlDefAttributeNode);
                }
            }
            else if (xamlDefAttributeNode.Name == FIELDMODIFIER)
            {
                if (!_pass2)
                {
                    _fieldModifier = _compiler.GetMemberAttributes(xamlDefAttributeNode.Value);
                    _isFieldModifierSet = true;
                }
            }
            // Some x: attributes are processed by the compiler, but are unknown
            // to the base XamlParser.  The compiler specific ones should not be passed
            // to XamlParser for processing, but the rest should be.
            else
            {
                bool isClass = xamlDefAttributeNode.Name == CLASS;
                bool isClassModifier = xamlDefAttributeNode.Name == CLASSMODIFIER;
                bool isTypeArgs = xamlDefAttributeNode.Name == XamlReaderHelper.DefinitionTypeArgs;
                bool isSubClass = xamlDefAttributeNode.Name == SUBCLASS;

                if (!isClass && !isClassModifier && !isTypeArgs && !isSubClass)
                {
                    base.WriteDefAttribute(xamlDefAttributeNode);
                }
                else if (!_isRootTag)
                {
                    ThrowException(SRID.DefinitionAttributeNotAllowed,
                                   MarkupCompiler.DefinitionNSPrefix,
                                   xamlDefAttributeNode.Name,
                                   xamlDefAttributeNode.LineNumber,
                                   xamlDefAttributeNode.LinePosition);
                }
                else if (isClass)
                {
                    if (_class == MarkupCompiler.DOT)
                    {
                        int index = xamlDefAttributeNode.Value.LastIndexOf(MarkupCompiler.DOTCHAR);
                        ThrowException(SRID.InvalidClassName,
                                       MarkupCompiler.DefinitionNSPrefix,
                                       CLASS,
                                       xamlDefAttributeNode.Value,
                                       index >= 0 ? "fully qualified " : string.Empty,
                                       xamlDefAttributeNode.LineNumber,
                                       xamlDefAttributeNode.LinePosition);
                    }

                    _class = string.Empty;
                }
                else if (isClassModifier)
                {
                    if (_classModifier == MarkupCompiler.DOT)
                    {
                        ThrowException(SRID.UnknownClassModifier,
                                       MarkupCompiler.DefinitionNSPrefix,
                                       xamlDefAttributeNode.Value,
                                       _compiler.Language,
                                       xamlDefAttributeNode.LineNumber,
                                       xamlDefAttributeNode.LinePosition);
                    }

                    _classModifier = string.Empty;
                }
                else if (isSubClass)
                {
                    if (_subClass == MarkupCompiler.DOT)
                    {
                        int index = xamlDefAttributeNode.Value.LastIndexOf(MarkupCompiler.DOTCHAR);
                        ThrowException(SRID.InvalidClassName,
                                       MarkupCompiler.DefinitionNSPrefix,
                                       SUBCLASS,
                                       xamlDefAttributeNode.Value,
                                       index >= 0 ? "fully qualified " : string.Empty,
                                       xamlDefAttributeNode.LineNumber,
                                       xamlDefAttributeNode.LinePosition);
                    }

                    _subClass = string.Empty;
                }
                else if (isTypeArgs)
                {
                    _compiler.AddGenericArguments(ParserContext, xamlDefAttributeNode.Value);
                }
            }
        }

/*
        // NOTE: Enable when Parser is ready to handle multiple errors w/o bailing out.
        internal override SerializationErrorAction ParseError(XamlParseException e)
        {
            _compiler.OnError(e);
            return SerializationErrorAction.Ignore;
        }
*/
        /// <summary>
        /// override of Write End Document
        /// </summary>
        public override void WriteDocumentEnd(XamlDocumentEndNode xamlEndDocumentNode)
        {
            if (BamlRecordWriter != null)
            {
                MemoryStream bamlMemStream = BamlRecordWriter.BamlStream as MemoryStream;
                Debug.Assert(bamlMemStream != null);
                base.WriteDocumentEnd(xamlEndDocumentNode);
                _compiler.GenerateBamlFile(bamlMemStream);
            }
        }

        internal override bool CanResolveLocalAssemblies()
        {
            return _pass2;
        }

        bool ProcessedRootElement
        {
            get { return _processedRootElement; }
            set { _processedRootElement = value; }
        }

        #endregion Overrides

        #region Data

        private MarkupCompiler      _compiler;
        private string              _name = null;
        private string              _class = string.Empty;
        private string              _subClass = string.Empty;
        private int                 _connectionId = 0;
        private bool                _pass2 = false;
        private bool                _isRootTag = false;
        private bool                _processedRootElement = false;
        private bool                _isSameScope = false;
        private bool                _isInternalRoot = false;
        private ArrayList           _events = null;
        private bool                _isFieldModifierSet = false;
        private CodeMemberField     _nameField = null;
        private string              _classModifier = string.Empty;
        private MemberAttributes    _fieldModifier = MemberAttributes.Assembly;

        private const string CLASS = "Class";
        private const string SUBCLASS = "Subclass";
        private const string CLASSMODIFIER = "ClassModifier";
        private const string FIELDMODIFIER = "FieldModifier";
        private const string STARTUPURI = "StartupUri";

        #endregion Data
    }

    #endregion ParserCallbacks
}
