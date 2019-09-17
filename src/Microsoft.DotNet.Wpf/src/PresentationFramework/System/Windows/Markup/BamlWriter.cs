// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***************************************************************************\
*
* Purpose:  Public api for writing baml records to a stream
*
\***************************************************************************/
using System;
using System.Xml;
using System.IO;
using System.Windows;
using System.Text;
using System.Collections;
using System.ComponentModel;
using MS.Internal.Utility;
using MS.Internal;

using System.Diagnostics;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Threading;

using MS.Utility;

namespace System.Windows.Markup
{
    /// <summary>
    /// Writes BAML records to Stream and exposes an XmlWriter-liker interface for BAML
    /// </summary>
    internal class BamlWriter : IParserHelper
    {
#region Constructor

        /// <summary>
        /// Create a BamlWriter on the passed stream.  The stream must be writable.
        /// </summary>
        public BamlWriter(
            Stream stream)
        {
            if (null == stream)
            {
                throw new ArgumentNullException( "stream" );
            }
            if (!stream.CanWrite)
            {
                throw new ArgumentException(SR.Get(SRID.BamlWriterBadStream));
            }
            
            _parserContext = new ParserContext();
            if (null == _parserContext.XamlTypeMapper) 
            {
                _parserContext.XamlTypeMapper = new BamlWriterXamlTypeMapper(XmlParserDefaults.GetDefaultAssemblyNames(),
                                                                             XmlParserDefaults.GetDefaultNamespaceMaps());
            }
            _xamlTypeMapper = _parserContext.XamlTypeMapper;
            _bamlRecordWriter = new BamlRecordWriter(stream, _parserContext, true);
            _startDocumentWritten = false;
            _depth = 0;
            _closed = false;
            _nodeTypeStack = new ParserStack();
            _assemblies = new Hashtable(7);
           _extensionParser = new MarkupExtensionParser((IParserHelper)this, _parserContext);
           _markupExtensionNodes = new ArrayList();
        }

  
#endregion Constructor

#region Close
        /// <summary>
        /// Close the underlying stream and terminate write operations.
        /// </summary>
        /// <remarks>
        /// Once Close() is called, the BamlWriter cannot be used again and all
        /// subsequent calls to BamlWriter will fail.
        /// </remarks>
        public void Close()
        {
            _bamlRecordWriter.BamlStream.Close();
            _closed = true;
        }
#endregion Close

#region IParserHelper

        string IParserHelper.LookupNamespace(string prefix)
        {
            return _parserContext.XmlnsDictionary[prefix];
        }

        bool IParserHelper.GetElementType(
                bool    extensionFirst,  
                string  localName,
                string  namespaceURI,
            ref string  assemblyName,
            ref string  typeFullName,
            ref Type    baseType,
            ref Type    serializerType)
        {
            bool result = false;

            assemblyName   = string.Empty;
            typeFullName   = string.Empty;
            serializerType = null;
            baseType       = null;

            // if no namespaceURI or local name don't bother
            if (null == namespaceURI || null == localName)
            {
                return false;
            }

            TypeAndSerializer typeAndSerializer = 
                _xamlTypeMapper.GetTypeAndSerializer(namespaceURI, localName, null);
            // If the normal type resolution fails, try the name with "Extension" added
            // so that we'll find MarkupExtension subclasses.
            if (typeAndSerializer == null)
            {
                typeAndSerializer = _xamlTypeMapper.GetTypeAndSerializer(namespaceURI, localName + "Extension", null);
            }

            if (typeAndSerializer != null &&
                typeAndSerializer.ObjectType != null)
            {
                serializerType = typeAndSerializer.SerializerType;
                baseType = typeAndSerializer.ObjectType;
                typeFullName = baseType.FullName;
                assemblyName = baseType.Assembly.FullName;
                result = true;
                
                Debug.Assert(null != assemblyName,"assembly name returned from GetBaseElement is null");
                Debug.Assert(null != typeFullName,"Type name returned from GetBaseElement is null");
            }

            return result;
        }
        

        bool IParserHelper.CanResolveLocalAssemblies()
        {
            return false;
        }
        

#endregion IParserHelper

#region Record Writing

        /// <summary>
        /// Write the start of document record, giving the baml version string
        /// </summary>
        /// <remarks>
        /// This must be the first call made when creating a new baml file.  This
        /// is needed to specify the start of the document and baml version.
        /// </remarks>
        public void WriteStartDocument()
        {
            if (_closed)
            {
                throw new InvalidOperationException(SR.Get(SRID.BamlWriterClosed));
            }
            if (_startDocumentWritten)
            {
                throw new InvalidOperationException(SR.Get(SRID.BamlWriterStartDoc));
            }
            
            XamlDocumentStartNode node = new XamlDocumentStartNode(0,0,_depth);
            _bamlRecordWriter.WriteDocumentStart(node);
            _startDocumentWritten = true;
            Push(BamlRecordType.DocumentStart);
        }

        /// <summary>
        /// Write the end of document record.
        /// </summary>
        /// <remarks>
        /// This must be the last call made when creating a new baml file.
        /// </remarks>
        public void WriteEndDocument()
        {
            VerifyEndTagState(BamlRecordType.DocumentStart, 
                              BamlRecordType.DocumentEnd);

            XamlDocumentEndNode node = new XamlDocumentEndNode(0,0,_depth);
            _bamlRecordWriter.WriteDocumentEnd(node);
        }

        /// <summary>
        /// Write the connection Id record.
        /// </summary>
        /// <remarks>
        /// A record that contains the connection Id of the current element
        /// for hooking up IDs and events.
        /// </remarks>
        public void WriteConnectionId(Int32 connectionId)
        {
            VerifyWriteState();

            _bamlRecordWriter.WriteConnectionId(connectionId);
        }


        /// <summary>
        /// Write the start of element record.
        /// </summary>
        /// <remarks>
        /// An element start marks the beginning of an object that exists
        /// in a tree structure.  This may the contents of a complex property,
        /// or an element in the logical tree.
        /// </remarks>
        public void WriteStartElement(
            string assemblyName,
            string typeFullName,
            bool isInjected,
            bool useTypeConverter)
        {
            VerifyWriteState();
            _dpProperty = null;
            _parserContext.PushScope();
            ProcessMarkupExtensionNodes();

            Type elementType = GetType(assemblyName, typeFullName);
            Type serializerType = _xamlTypeMapper.GetXamlSerializerForType(elementType);
            Push(BamlRecordType.ElementStart, elementType);
            XamlElementStartNode node = new XamlElementStartNode(
                                               0,
                                               0,
                                               _depth++,
                                               assemblyName,
                                               typeFullName,
                                               elementType,
                                               serializerType);  
            node.IsInjected = isInjected;
            node.CreateUsingTypeConverter = useTypeConverter;
            
            _bamlRecordWriter.WriteElementStart(node);
        }

        /// <summary>
        /// Write the end of element record.
        /// </summary>
        /// <remarks>
        /// An element end marks the ending of an object that exists
        /// in a tree structure.  This may the contents of a complex property,
        /// or an element in the logical tree.
        /// </remarks>
        public void WriteEndElement()
        {
            VerifyEndTagState(BamlRecordType.ElementStart, 
                              BamlRecordType.ElementEnd);
            ProcessMarkupExtensionNodes();
            
            XamlElementEndNode node = new XamlElementEndNode(
                                               0,
                                               0,
                                               --_depth);
            _bamlRecordWriter.WriteElementEnd(node);
            _parserContext.PopScope();
        }
        
        /// <summary>
        /// Write the start of constructor section that follows the start of an element.
        /// </summary>
        public void WriteStartConstructor()
        {
            VerifyWriteState();
            Push(BamlRecordType.ConstructorParametersStart);
            
            XamlConstructorParametersStartNode node = new XamlConstructorParametersStartNode(
                                               0,
                                               0,
                                               --_depth);
            _bamlRecordWriter.WriteConstructorParametersStart(node);
        }

        /// <summary>
        /// Write the end of constructor section that follows the start of an element.
        /// </summary>
        public void WriteEndConstructor()
        {
            VerifyEndTagState(BamlRecordType.ConstructorParametersStart, 
                              BamlRecordType.ConstructorParametersEnd);
            
            XamlConstructorParametersEndNode node = new XamlConstructorParametersEndNode(
                                               0,
                                               0,
                                               --_depth);
            _bamlRecordWriter.WriteConstructorParametersEnd(node);
        }
        
        /// <summary>
        /// Write simple property information to baml.
        /// </summary>
        /// <remarks>
        /// If the type of this property supports
        /// custom serialization using a XamlSerializer and custom serialization
        /// is chosen, then write out a 
        /// Custom serialization record.  Note that for custom serialization
        /// to work, the assembly that contains the property's type must be 
        /// loaded. If custom serialization is not chosen, then
        /// write out a 'normal' record, which will cause type conversion to happen
        /// at load time from the stored string.
        /// </remarks>
        public void WriteProperty(
            string             assemblyName,
            string             ownerTypeFullName,
            string             propName,
            string             value,
            BamlAttributeUsage propUsage)
        {
            VerifyWriteState();
            
            BamlRecordType parentType = PeekRecordType();
            if (parentType != BamlRecordType.ElementStart)
            {
                throw new InvalidOperationException(SR.Get(SRID.BamlWriterNoInElement,
                                                           "WriteProperty",
                                                           parentType.ToString()));
            }

            object dpOrPi;
            Type   declaringType;
            GetDpOrPi(assemblyName, ownerTypeFullName, propName, out dpOrPi, out declaringType);

            // Check if the value is a MarkupExtension.  If so it must be expanded into
            // a series of baml records.  Otherwise just write out the property.
            AttributeData data = _extensionParser.IsMarkupExtensionAttribute(
                                                        declaringType,    
                                                        propName,       
                                                    ref value,
                                                        0,       // No line numbers for baml
                                                        0,
                                                        0,
                                                        dpOrPi);

            if (data == null)
            {
                XamlPropertyNode propNode = new XamlPropertyNode(
                                            0,
                                            0,
                                            _depth,
                                            dpOrPi,   
                                            assemblyName,
                                            ownerTypeFullName,
                                            propName,
                                            value,
                                            propUsage,
                                            false);

                Type propType = XamlTypeMapper.GetPropertyType(dpOrPi);
                if (propType == typeof(DependencyProperty))
                {
                    Type ownerType = null;
                    _dpProperty = XamlTypeMapper.ParsePropertyName(_parserContext, value, ref ownerType);

                    if (_bamlRecordWriter != null && _dpProperty != null)
                    {
                        short typeId;
                        short propertyId = _parserContext.MapTable.GetAttributeOrTypeId(_bamlRecordWriter.BinaryWriter,
                                                                                        ownerType,
                                                                                        _dpProperty.Name,
                                                                                        out typeId);

                        if (propertyId < 0)
                        {
                            propNode.ValueId = propertyId;
                            propNode.MemberName = null;
                        }
                        else
                        {
                            propNode.ValueId = typeId;
                            propNode.MemberName = _dpProperty.Name;
                        }
                    }
                }
                else if (_dpProperty != null)
                {
                    propNode.ValuePropertyType = _dpProperty.PropertyType;
                    propNode.ValuePropertyMember = _dpProperty;
                    propNode.ValuePropertyName = _dpProperty.Name;
                    propNode.ValueDeclaringType = _dpProperty.OwnerType;
                    string propAssemblyName = _dpProperty.OwnerType.Assembly.FullName;
                    _dpProperty = null;
                }

                _bamlRecordWriter.WriteProperty(propNode);            
            }
            else
            {
                if (data.IsSimple)
                {
                    if (data.IsTypeExtension)
                    {
                        Type typeValue = _xamlTypeMapper.GetTypeFromBaseString(data.Args,
                                                                               _parserContext,
                                                                               true);
                        Debug.Assert(typeValue != null);

                        XamlPropertyWithTypeNode xamlPropertyWithTypeNode =
                            new XamlPropertyWithTypeNode(0,
                                                         0,
                                                         _depth,
                                                         dpOrPi,   
                                                         assemblyName,
                                                         ownerTypeFullName,
                                                         propName,
                                                         typeValue.FullName,
                                                         typeValue.Assembly.FullName,
                                                         typeValue,   
                                                         string.Empty,
                                                         string.Empty);

                        _bamlRecordWriter.WritePropertyWithType(xamlPropertyWithTypeNode);
                    }
                    else
                    {
                        XamlPropertyWithExtensionNode xamlPropertyWithExtensionNode =
                            new XamlPropertyWithExtensionNode(0,
                                                              0,
                                                              _depth,
                                                              dpOrPi,
                                                              assemblyName,
                                                              ownerTypeFullName,
                                                              propName,
                                                              data.Args,
                                                              data.ExtensionTypeId,
                                                              data.IsValueNestedExtension,
                                                              data.IsValueTypeExtension);

                        _bamlRecordWriter.WritePropertyWithExtension(xamlPropertyWithExtensionNode);
                    }
                }
                else
                {
                    _extensionParser.CompileAttribute(
                                            _markupExtensionNodes, data);
                }
            }
        }

        /// <summary>
        /// Write Content Property Record.
        /// </summary>
        public void WriteContentProperty(
            string             assemblyName,
            string             ownerTypeFullName,
            string             propName )
        {
            object dpOrPi;
            Type   declaringType;
            GetDpOrPi(assemblyName, ownerTypeFullName, propName, out dpOrPi, out declaringType);

            XamlContentPropertyNode CpaNode = new XamlContentPropertyNode(
                                                    0, 0, _depth,
                                                    dpOrPi,
                                                    assemblyName,
                                                    ownerTypeFullName,
                                                    propName);
            _bamlRecordWriter.WriteContentProperty( CpaNode );
        }

        /// <summary>
        /// Write xml namespace declaration information to baml.
        /// </summary>
        /// <remarks>
        /// This is used for setting the default namespace, or for mapping
        /// a namespace prefix (or localName) to a namespace.  If prefix is 
        /// an empty string, then this sets the default namespace.
        /// </remarks>
        public void WriteXmlnsProperty(
            string  localName,
            string  xmlNamespace)
        {
            VerifyWriteState();
            
            BamlRecordType parentType = PeekRecordType();
            if (parentType != BamlRecordType.ElementStart &&
                parentType != BamlRecordType.PropertyComplexStart &&
                parentType != BamlRecordType.PropertyArrayStart &&
                parentType != BamlRecordType.PropertyIListStart &&
                parentType != BamlRecordType.PropertyIDictionaryStart)
            {
                throw new InvalidOperationException(SR.Get(SRID.BamlWriterBadXmlns,
                                                           "WriteXmlnsProperty",
                                                           parentType.ToString()));
            }
            
            XamlXmlnsPropertyNode xmlnsNode = new XamlXmlnsPropertyNode(
                                                    0,
                                                    0,
                                                    _depth,
                                                    localName,
                                                    xmlNamespace);
        
            _bamlRecordWriter.WriteNamespacePrefix(xmlnsNode);
            _parserContext.XmlnsDictionary[localName] = xmlNamespace;
        }

        /// <summary>
        /// Write an attribute in the Definition namespace.  
        /// </summary>
        /// <remarks>
        /// This is really a processing directive, rather than an actual property.
        /// It is used to define keys for dictionaries, resource references,
        /// language declarations, base class names for tags, etc.
        /// </remarks>
        public void WriteDefAttribute(
            string name,
            string value)
        {
            VerifyWriteState();
            
            BamlRecordType parentType = PeekRecordType();
            if (parentType != BamlRecordType.ElementStart &&
                name != "Uid" ) // Parser's supposed to ignore x:Uid everywhere
            {
                throw new InvalidOperationException(SR.Get(SRID.BamlWriterNoInElement,
                                                           "WriteDefAttribute",
                                                           parentType.ToString()));
            }
            else if (name == XamlReaderHelper.DefinitionName)
            {
                // Check if this is a MarkupExtension, and if so expand it in xaml so that
                // it represents a key tree.
                DefAttributeData data = _extensionParser.IsMarkupExtensionDefAttribute(
                                                            PeekElementType(),    
                                                            ref value, 0, 0, 0);

                if (data != null)
                {
                    if (name != XamlReaderHelper.DefinitionName)
                    {
                        data.IsSimple = false;
                    }
                    if (data.IsSimple)
                    {
                        // If the MarkupExtension does not expand out into a complex property
                        // subtree, but can be handled inline with other properties, then
                        // process it immediately in place of the normal property
                        int colonIndex = data.Args.IndexOf(':');
                        string prefix = string.Empty;
                        string typeName = data.Args;
                        if (colonIndex > 0)
                        {
                            prefix = data.Args.Substring(0, colonIndex);
                            typeName = data.Args.Substring(colonIndex+1);
                        }

                        string valueNamespaceURI = _parserContext.XmlnsDictionary[prefix];
                        string valueAssemblyName = string.Empty;
                        string valueTypeFullName = string.Empty;
                        Type   valueElementType = null;
                        Type   valueSerializerType = null;

                        bool resolvedTag = ((IParserHelper)this).GetElementType(false, typeName, 
                                                        valueNamespaceURI,ref valueAssemblyName,
                                                        ref valueTypeFullName, ref valueElementType, 
                                                        ref valueSerializerType);

                        // If we can't resolve a simple TypeExtension value at compile time,
                        // then write it out as a normal type extension and wait until runtime
                        // since the type may not be visible now.
                        if (resolvedTag)
                        {
                            XamlDefAttributeKeyTypeNode defKeyNode = new XamlDefAttributeKeyTypeNode(
                                                                    0,
                                                                    0,
                                                                    _depth,
                                                                    valueTypeFullName,  
                                                                    valueElementType.Assembly.FullName,  
                                                                    valueElementType);
                            _bamlRecordWriter.WriteDefAttributeKeyType(defKeyNode);
                        }
                        else
                        {
                            data.IsSimple = false;
                            data.Args += "}";
                        }
                    }
                    if (!data.IsSimple)
                    {
                        _extensionParser.CompileDictionaryKey(
                                            _markupExtensionNodes, data);
                    }
                    return;
                }
            }
            
            XamlDefAttributeNode defNode = new XamlDefAttributeNode(
                                                    0,
                                                    0,
                                                    _depth,
                                                    name,
                                                    value);
        
            _bamlRecordWriter.WriteDefAttribute(defNode);
        }

        /// <summary>
        /// Write an attribute in the PresentationOptions namespace.  
        /// </summary>
        /// <remarks>
        /// This is really a processing directive, rather than an actual property.
        /// It is used to define WPF-specific parsing options (e.g., PresentationOptions:Freeze)
        /// </remarks>
        public void WritePresentationOptionsAttribute(
            string name,
            string value)
        {
            VerifyWriteState();
            
            XamlPresentationOptionsAttributeNode defNode = new XamlPresentationOptionsAttributeNode(
                                                    0,
                                                    0,
                                                    _depth,
                                                    name,
                                                    value);
        
            _bamlRecordWriter.WritePresentationOptionsAttribute(defNode);
        }        

        /// <summary>
        /// Write the start of a complex property
        /// </summary>
        /// <remarks>
        /// A complex property start marks the beginning of an object that exists
        /// in a property on an element in the tree.
        /// </remarks>
        public void WriteStartComplexProperty(
            string assemblyName,
            string ownerTypeFullName,
            string propName)
        {
            VerifyWriteState();
            _parserContext.PushScope();
            ProcessMarkupExtensionNodes();

            object dpOrPi;
            Type   ownerType;
            Type   propertyType = null;
            bool   propertyCanWrite = true;
            
            GetDpOrPi(assemblyName, ownerTypeFullName, propName, out dpOrPi, out ownerType);
            if (dpOrPi == null)
            {
                MethodInfo mi = GetMi(assemblyName, ownerTypeFullName, propName, out ownerType);
                if (mi != null)
                {
                    XamlTypeMapper.GetPropertyType(mi, out propertyType, out propertyCanWrite);
                }
            }
            else
            {
                propertyType = XamlTypeMapper.GetPropertyType(dpOrPi);
                PropertyInfo pi = dpOrPi as PropertyInfo;
                if (pi != null)
                {
                    propertyCanWrite = pi.CanWrite;
                }
                else
                {
                    DependencyProperty dp = dpOrPi as DependencyProperty;
                    if (dp != null)
                    {
                        propertyCanWrite = !dp.ReadOnly;
                    }
                }
            }

            // Based on the type of the property, we could write this as 
            // a complex IList, Array, IDictionary or a regular complex property.
            // NOTE:  The order this is checked in must the same order as the
            //        XamlReaderHelper.CompileComplexProperty so that we have
            //        the same property behavior (eg - if something implements
            //        IDictionary and IList, it is treated as an IList)
            if (propertyType == null)
            {
                // Unknown complex properties are written out using the string
                // information passed.  This applies to things like Set.Value
                // and other style related DPs
                Push(BamlRecordType.PropertyComplexStart);
                XamlPropertyComplexStartNode propertyStart = new XamlPropertyComplexStartNode(
                                                                 0,
                                                                 0,
                                                                 _depth++,
                                                                 null,
                                                                 assemblyName,
                                                                 ownerTypeFullName,
                                                                 propName);
                _bamlRecordWriter.WritePropertyComplexStart(propertyStart);
            }
            else
            {
                BamlRecordType recordType = BamlRecordManager.GetPropertyStartRecordType(propertyType, propertyCanWrite);
                Push(recordType);
                
                switch (recordType)
                {
                    case BamlRecordType.PropertyArrayStart:
                    {
                        XamlPropertyArrayStartNode arrayStart = new XamlPropertyArrayStartNode(
                                                                    0,
                                                                    0,
                                                                    _depth++,
                                                                    dpOrPi,
                                                                    assemblyName,
                                                                    ownerTypeFullName,
                                                                    propName);
                        _bamlRecordWriter.WritePropertyArrayStart(arrayStart);
                        break;
                    }
                    case BamlRecordType.PropertyIDictionaryStart:
                    {
                         XamlPropertyIDictionaryStartNode dictionaryStart = 
                                                  new XamlPropertyIDictionaryStartNode(
                                                              0,
                                                              0,
                                                              _depth++,
                                                              dpOrPi,
                                                              assemblyName,
                                                              ownerTypeFullName,
                                                              propName);
                        _bamlRecordWriter.WritePropertyIDictionaryStart(dictionaryStart);
                        break;
                    }
                    case BamlRecordType.PropertyIListStart:
                    {
                        XamlPropertyIListStartNode listStart = new XamlPropertyIListStartNode(
                                                                        0,
                                                                        0,
                                                                        _depth++,
                                                                        dpOrPi,
                                                                        assemblyName,
                                                                        ownerTypeFullName,
                                                                        propName);
                        _bamlRecordWriter.WritePropertyIListStart(listStart);
                        break;
                    }
                    default: // PropertyComplexStart
                    {
                         XamlPropertyComplexStartNode node = new XamlPropertyComplexStartNode(
                                                       0,
                                                       0,
                                                       _depth++,
                                                       dpOrPi,
                                                       assemblyName,
                                                       ownerTypeFullName,
                                                       propName);
                    
                        _bamlRecordWriter.WritePropertyComplexStart(node);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Write the end of the complex property
        /// </summary>
        public void WriteEndComplexProperty()
        {
            VerifyWriteState();

            // The type of end record written depends on what the start record
            // was.  This is held on the _nodeTypeStack
            BamlRecordType startTagType = Pop();

            switch (startTagType)
            {
                case BamlRecordType.PropertyArrayStart:
                    XamlPropertyArrayEndNode arrayEnd = 
                                      new XamlPropertyArrayEndNode(
                                                  0, 
                                                  0, 
                                                  --_depth);
                    _bamlRecordWriter.WritePropertyArrayEnd(arrayEnd);
                    break;
                    
                case BamlRecordType.PropertyIListStart:
                    XamlPropertyIListEndNode listEnd = 
                                      new XamlPropertyIListEndNode(
                                                  0, 
                                                  0, 
                                                  --_depth);
                    _bamlRecordWriter.WritePropertyIListEnd(listEnd);
                    break;
        
                case BamlRecordType.PropertyIDictionaryStart:
                    XamlPropertyIDictionaryEndNode dictionaryEnd = 
                                      new XamlPropertyIDictionaryEndNode(
                                                  0, 
                                                  0, 
                                                  --_depth);
                    _bamlRecordWriter.WritePropertyIDictionaryEnd(dictionaryEnd);
                    break;
                    
                case BamlRecordType.PropertyComplexStart:
                    XamlPropertyComplexEndNode complexEnd = 
                                       new XamlPropertyComplexEndNode(
                                                  0, 
                                                  0, 
                                                  --_depth);
                    _bamlRecordWriter.WritePropertyComplexEnd(complexEnd);
                    break;

                default:
                    throw new InvalidOperationException(
                                    SR.Get(SRID.BamlWriterBadScope,
                                           startTagType.ToString(),
                                           BamlRecordType.PropertyComplexEnd.ToString()));
            }                        
            _parserContext.PopScope();
        }

        /// <summary>
        /// Write a literal content record to baml stream
        /// </summary>
        public void WriteLiteralContent(
            string contents)
        {
            VerifyWriteState();
            ProcessMarkupExtensionNodes();

            XamlLiteralContentNode literalContent = new XamlLiteralContentNode(
                                                               0,
                                                               0,
                                                               _depth,
                                                               contents);
            _bamlRecordWriter.WriteLiteralContent(literalContent);
        }

        /// <summary>
        /// Write a mapping processing instruction to baml stream
        /// </summary>
        public void WritePIMapping(
            string    xmlNamespace,
            string    clrNamespace,
            string    assemblyName)
        {
            VerifyWriteState();
            ProcessMarkupExtensionNodes();

            XamlPIMappingNode piMapping = new XamlPIMappingNode(
                                                     0,
                                                     0,
                                                     _depth,
                                                     xmlNamespace,
                                                     clrNamespace,
                                                     assemblyName);
            if (!_xamlTypeMapper.PITable.Contains(xmlNamespace))
            {
                ClrNamespaceAssemblyPair mapping = new ClrNamespaceAssemblyPair(clrNamespace, assemblyName);
                _xamlTypeMapper.PITable.Add(xmlNamespace, mapping);
            }
            // Write it out anyway.  It is redundant but we are being asked to write it so write it.
            // In the round trip case this will preserve the exact file data.
            _bamlRecordWriter.WritePIMapping(piMapping);
        }

        /// <summary>
        /// Write text content into baml stream
        /// </summary>
        public void WriteText(
            string textContent,
            string typeConverterAssemblyName,
            string typeConverterName)
        {
            VerifyWriteState();
            ProcessMarkupExtensionNodes();

            Type typeConverter=null;
            if (!String.IsNullOrEmpty(typeConverterName))
            {
                typeConverter = GetType(typeConverterAssemblyName, typeConverterName);
            }

            XamlTextNode textNode = new XamlTextNode(
                                             0,
                                             0,
                                             _depth,
                                             textContent,
                                             typeConverter);
            _bamlRecordWriter.WriteText(textNode);
        }

        /// <summary>
        /// Write a routed event record to BAML.  
        /// </summary>
        /// <remarks>
        /// The Avalon parser does not process routed event records itself and will
        /// throw an exception if it encounters a routed event record in BAML.  It
        /// is included here for completeness and future expandability.
        /// </remarks>
        public void WriteRoutedEvent(
            string     assemblyName,
            string     ownerTypeFullName,
            string     eventIdName,
            string     handlerName)
        {
#if EVENTSUPPORT            
            VerifyWriteState();

            XamlRoutedEventNode eventNode = new XamlRoutedEventNode(
                                                      0,
                                                      0,
                                                      _depth,
                                                      null,
                                                      assemblyName,
                                                      ownerTypeFullName,
                                                      eventIdName,
                                                      handlerName);
            _bamlRecordWriter.WriteRoutedEvent(eventNode);
#else
            throw new NotSupportedException(SR.Get(SRID.ParserBamlEvent, eventIdName));
#endif

        }

        /// <summary>
        /// Write an event record to BAML.  
        /// </summary>
        /// <remarks>
        /// The Avalon parser does not process event records itself and will
        /// throw an exception if it encounters  an event record in BAML.  It
        /// is included here for completeness and future expandability.
        /// </remarks>
        public void WriteEvent(
            string    eventName,
            string    handlerName)
        {
#if EVENTSUPPORT            
            VerifyWriteState();

            XamlClrEventNode eventNode = new XamlClrEventNode(
                                                  0,
                                                  0,
                                                  _depth,
                                                  eventName,
                                                  null,
                                                  handlerName);
            _bamlRecordWriter.WriteClrEvent(eventNode);
#else
            throw new NotSupportedException(SR.Get(SRID.ParserBamlEvent, eventName));
#endif
        }

#endregion Record Writing

#region Internal Methods

    /***************************************************************************\
    *
    * BamlWriter.ProcessMarkupExtensionNodes
    *
    * Write out baml records for all the xamlnodes contained in the buffered
    * markup extension list.
    * NOTE:  This list must contain only xamlnodes that are known to be part of
    *        MarkupExtensions.  This is NOT a general method to handle all types
    *        of xaml nodes.
    *
    \***************************************************************************/

    private void ProcessMarkupExtensionNodes()
    {
        for (int i=0; i < _markupExtensionNodes.Count; i++)
        {
            XamlNode node = _markupExtensionNodes[i] as XamlNode;
            switch (node.TokenType)
            {
                case XamlNodeType.ElementStart:
                    _bamlRecordWriter.WriteElementStart((XamlElementStartNode)node);
                    break;
                case XamlNodeType.ElementEnd:
                    _bamlRecordWriter.WriteElementEnd((XamlElementEndNode)node);
                    break;
                case XamlNodeType.KeyElementStart:
                    _bamlRecordWriter.WriteKeyElementStart((XamlKeyElementStartNode)node);
                    break;
                case XamlNodeType.KeyElementEnd:
                    _bamlRecordWriter.WriteKeyElementEnd((XamlKeyElementEndNode)node);
                    break;
                case XamlNodeType.Property:
                    _bamlRecordWriter.WriteProperty((XamlPropertyNode)node);
                    break;
                case XamlNodeType.PropertyWithExtension:
                    _bamlRecordWriter.WritePropertyWithExtension((XamlPropertyWithExtensionNode)node);
                    break;
                case XamlNodeType.PropertyWithType:
                    _bamlRecordWriter.WritePropertyWithType((XamlPropertyWithTypeNode)node);
                    break;
                case XamlNodeType.PropertyComplexStart:
                    _bamlRecordWriter.WritePropertyComplexStart((XamlPropertyComplexStartNode)node);
                    break;
                case XamlNodeType.PropertyComplexEnd:
                    _bamlRecordWriter.WritePropertyComplexEnd((XamlPropertyComplexEndNode)node);
                    break;
                case XamlNodeType.Text:
                    _bamlRecordWriter.WriteText((XamlTextNode)node);
                    break;
                case XamlNodeType.EndAttributes:
                    _bamlRecordWriter.WriteEndAttributes((XamlEndAttributesNode)node);
                    break;
                case XamlNodeType.ConstructorParametersStart:
                    _bamlRecordWriter.WriteConstructorParametersStart((XamlConstructorParametersStartNode)node);
                    break;
                case XamlNodeType.ConstructorParametersEnd:
                    _bamlRecordWriter.WriteConstructorParametersEnd((XamlConstructorParametersEndNode)node);
                    break;
                default:
                    throw new InvalidOperationException(SR.Get(SRID.BamlWriterUnknownMarkupExtension));
            }
        }
        _markupExtensionNodes.Clear();
    }

    /***************************************************************************\
    *
    * BamlWriter.VerifyWriteState
    *
    * Verify that we are in a good state to perform a record write.  Throw
    * appropriate exceptions if not.
    *
    \***************************************************************************/
    
    private void VerifyWriteState()
    {
        if (_closed)
        {
            throw new InvalidOperationException(SR.Get(SRID.BamlWriterClosed));
        }
        if (!_startDocumentWritten)
        {
            throw new InvalidOperationException(SR.Get(SRID.BamlWriterStartDoc));
        }
    }

    /***************************************************************************\
    *
    * BamlWriter.VerifyEndTagState
    *
    * Verify that we are in a good state to perform a record write and that
    * the xamlnodetype on the node type stack is of the expected type.  This
    * is called when an end tag record is written
    *
    \***************************************************************************/
    
    private void VerifyEndTagState(
        BamlRecordType   expectedStartTag,
        BamlRecordType   endTagBeingWritten)
    {
        VerifyWriteState();

        BamlRecordType startTagState = Pop();
        if (startTagState != expectedStartTag)
        {
            throw new InvalidOperationException(SR.Get(SRID.BamlWriterBadScope,
                                                       startTagState.ToString(),
                                                       endTagBeingWritten.ToString()));
        }
    }

    /***************************************************************************\
    *
    * BamlWriter.GetAssembly
    *
    * Get the Assembly given a name.  This uses the LoadWrapper to load the
    * assembly from the current directory.  
    * NOTE:  Assembly paths are not currently supported, but may be in the
    *        future if the need arises.
    *
    \***************************************************************************/

    private Assembly GetAssembly(string assemblyName)
    {
        Assembly assy = _assemblies[assemblyName] as Assembly;
        if (assy == null)
        {
            assy = ReflectionHelper.LoadAssembly(assemblyName, null);
            if (assy == null)
            {
                throw new ArgumentException(SR.Get(SRID.BamlWriterBadAssembly, 
                                                   assemblyName));
            }
            else
            {
                _assemblies[assemblyName] = assy;
            }
        }

        return assy;
    }

    /***************************************************************************\
    *
    * BamlWriter.GetType
    *
    * Get the Type given an assembly name where the type is declared and the
    * type's fully qualified name
    *
    \***************************************************************************/
    
    private Type GetType(
        string assemblyName,
        string typeFullName)
    {
        // Get the assembly that contains the type of the element
        Assembly assembly = GetAssembly(assemblyName);
        
        // Now see if the type is declared in this assembly
        Type objectType = assembly.GetType(typeFullName);

        // Note that null objectType is allowed, since this may be something like
        // a <Set> element in a style, which is mapped to a element tag
        return objectType;
    }

    /***************************************************************************\
    *
    * BamlWriter.GetDpOrPi
    *
    * Get the DependencyProperty or the PropertyInfo that corresponds to the 
    * passed property name on the passed owner type (or type where the
    * property is declared).
    *
    \***************************************************************************/

    private object GetDpOrPi(
        Type    ownerType,
        string  propName)
    {
        // Now that we have the type, see if this is a DependencyProperty or
        // a PropertyInfo.  Note that ownerType may be null for fake properties
        // like those associated with Styles.  Note also that the property may
        // not resolve to a known DP.  This is allowed for fake properties like
        // those contained in Styles, such as Set.Value.
        object dpOrPi = null;

#if !PBTCOMPILER
        if (ownerType != null)
        {
            dpOrPi = DependencyProperty.FromName(propName, ownerType);
            if (dpOrPi == null)
            {
                PropertyInfo mostDerived = null;
                MemberInfo[] infos = ownerType.GetMember(propName, MemberTypes.Property, BindingFlags.Instance | BindingFlags.Public);
                foreach(PropertyInfo pi in infos)
                {
                    if(pi.GetIndexParameters().Length == 0)
                    {
                        if(mostDerived == null || mostDerived.DeclaringType.IsAssignableFrom(pi.DeclaringType))
                        {
                            mostDerived = pi;
                        }
                    }
                }
                dpOrPi = mostDerived;
            }
        }
#else
        if (ownerType != null)
        {

            dpOrPi = KnownTypes.Types[(int)KnownElements.DependencyProperty].InvokeMember("FromName", 
                                             BindingFlags.InvokeMethod, 
                                             null, 
                                             null, 
                                             new object[] {propName, ownerType} );

            if (dpOrPi == null)
            {
                dpOrPi = ownerType.GetProperty(propName, 
                                   BindingFlags.Instance | BindingFlags.Public);
            }
        }
#endif

        return dpOrPi;
    }
    
    /***************************************************************************\
    *
    * BamlWriter.GetDpOrPi
    *
    * Get the DependencyProperty or the PropertyInfo that corresponds to the 
    * passed property name on the passed owner type name.  This typename is
    * first resolved to a type.
    *
    \***************************************************************************/

    private void GetDpOrPi(
            string  assemblyName,
            string  ownerTypeFullName,
            string  propName,
        out object  dpOrPi,
        out Type    ownerType)
    {
        // If there is no valid owner or assembly, then we can't resolve the type,
        // so just return null.  This can occur if we are writing 'fake' properties
        // that are used for things such as Style property triggers.
        if (assemblyName == string.Empty || ownerTypeFullName == string.Empty)
        {
            dpOrPi = null;
            ownerType = null;
        }
        else
        {
            ownerType = GetType(assemblyName, ownerTypeFullName);
            dpOrPi = GetDpOrPi(ownerType, propName);
        }
    }

    private MethodInfo GetMi(Type ownerType, string propName)
    {
        MethodInfo memberInfo = null;

        memberInfo = ownerType.GetMethod("Set" + propName,
                                            BindingFlags.Public |
                                            BindingFlags.Static |
                                            BindingFlags.FlattenHierarchy);
        if (memberInfo != null && ((MethodInfo)memberInfo).GetParameters().Length != 2)
        {
            memberInfo = null;
        }

        // Try read-only case (Getter only)
        if (memberInfo == null)
        {
            memberInfo = ownerType.GetMethod("Get" + propName,
                                                BindingFlags.Public |
                                                BindingFlags.Static |
                                                BindingFlags.FlattenHierarchy);
            if (memberInfo != null && ((MethodInfo)memberInfo).GetParameters().Length != 1)
            {
                memberInfo = null;
            }
        }
        return memberInfo;
    }

    private MethodInfo GetMi(
            string assemblyName,
            string ownerTypeFullName,
            string propName,
            out Type ownerType)
    {
        MethodInfo mi = null;

        // If there is no valid owner or assembly, then we can't resolve the type,
        // so just return null.  This can occur if we are writing 'fake' properties
        // that are used for things such as Style property triggers.
        if (assemblyName == string.Empty || ownerTypeFullName == string.Empty)
        {
            mi = null;
            ownerType = null;
        }
        else
        {
            ownerType = GetType(assemblyName, ownerTypeFullName);
            mi = GetMi(ownerType, propName);
        }
        return mi;
    }


    // Helper methods for dealing with the node stack

    // Push a new record type on the stack.  If we have not generated an 
    // end attributes write operation for the current item on the stack, and
    // that item is a start element, then now is the time to do it.
    private void Push(BamlRecordType recordType)
    {
        CheckEndAttributes();
        _nodeTypeStack.Push(new WriteStackNode(recordType));
    }
    
    private void Push(BamlRecordType recordType, Type elementType)
    {
        CheckEndAttributes();
        _nodeTypeStack.Push(new WriteStackNode(recordType, elementType));
    }

    // Pop an item off the node stack and return its type.
    private BamlRecordType Pop()
    {
        WriteStackNode stackNode = _nodeTypeStack.Pop() as WriteStackNode;
        Debug.Assert(stackNode != null);
        return stackNode.RecordType;
    }

    // Return the record type on the top of the stack
    private BamlRecordType PeekRecordType()
    {
        WriteStackNode stackNode = _nodeTypeStack.Peek() as WriteStackNode;
        Debug.Assert(stackNode != null);
        return stackNode.RecordType;
    }
    
    // Return the element type on the top of the stack
    private Type PeekElementType()
    {
        WriteStackNode stackNode = _nodeTypeStack.Peek() as WriteStackNode;
        Debug.Assert(stackNode != null);
        return stackNode.ElementType;
    }

    // Check if we have to insert an EndAttributes xaml node at the end
    // of an element start tag.
    private void CheckEndAttributes()
    {
        if (_nodeTypeStack.Count > 0)
        {
            WriteStackNode parentNode = _nodeTypeStack.Peek() as WriteStackNode;
            if (!parentNode.EndAttributesReached &&
                parentNode.RecordType == BamlRecordType.ElementStart)
            {
                XamlEndAttributesNode node = new XamlEndAttributesNode(
                                                   0,
                                                   0,
                                                   _depth,
                                                   false);
                _bamlRecordWriter.WriteEndAttributes(node);
            }
            parentNode.EndAttributesReached = true;
        }
    }


#endregion Internal Methods

#region Data

        // Item pushed on a node stack to keep track for matching
        // end tags and for determining when the end of the 
        // start tag has been reached for generating an end attribute write.
        private class WriteStackNode
        {
            public WriteStackNode(
                BamlRecordType   recordType)
            {
                _recordType = recordType;
                _endAttributesReached = false;
            }
            
            public WriteStackNode(
                BamlRecordType   recordType,
                Type             elementType) : this(recordType)
            {
                _elementType = elementType;
            }

            public BamlRecordType RecordType
            {
                get { return _recordType; }
            }

            public bool EndAttributesReached 
            {
                get { return _endAttributesReached; }
                set { _endAttributesReached = value; }
            }

            public Type ElementType
            {
                get { return _elementType; }
            }

            bool           _endAttributesReached;
            BamlRecordType _recordType;
            Type           _elementType;
        }

        // Writer that actually writes BamlRecords to a stream.
        BamlRecordWriter      _bamlRecordWriter;
    
        // True if the DocumentStart record has been written to the stream.
        bool                  _startDocumentWritten;

        // The depth of the element tree, including complex properties
        int                   _depth;

        // True if Close() has been called.
        bool                  _closed;

        // If a custom property is of Type DependencyProperty, this is used to provide
        // info about the Type of value for such a property in order to write it out in
        // an optimized form by its custom serializer.
        DependencyProperty    _dpProperty;

        // Stack of the type of nodes written to the baml stream.  This is 
        // used for end-tag matching and basic structure checking.  This
        // contains WriteStackNode objects.
        ParserStack           _nodeTypeStack;

        // Cache of assemblies that are needed for type resolutions when
        // doingIBamlSerialize
        Hashtable             _assemblies;
    
        // XamlTypeMapper used by this writer
        XamlTypeMapper        _xamlTypeMapper;

        // ParserContext for this writer
        ParserContext         _parserContext;

        // The helper class that handles parsing of MarkupExtension values.
        MarkupExtensionParser _extensionParser;

        // Buffered XamlNodes that occur when a property with a MarkupExtension value
        // is written and expanded into a complex property subtree.
        ArrayList             _markupExtensionNodes;

#endregion Data
    }

    internal class BamlWriterXamlTypeMapper : XamlTypeMapper
    {
        internal BamlWriterXamlTypeMapper(
            string[] assemblyNames,
            NamespaceMapEntry[] namespaceMaps) : base(assemblyNames, namespaceMaps)
        {
        }

        /// <summary>
        /// Allows BamlWriter to allow access to legitimate internal types
        /// </summary>
        /// <param name="type">The internal type</param>
        /// <returns>
        /// Always Returns true by default, since this is used by localization
        /// </returns>
        sealed protected override bool AllowInternalType(Type type)
        {
            return true;
        }
    }
}
