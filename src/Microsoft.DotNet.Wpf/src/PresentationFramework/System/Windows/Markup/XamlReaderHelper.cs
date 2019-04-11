// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***************************************************************************\
*
* Purpose:  Tokenizer for taking an xml file for generate XAML nodes.
*           and enforcing XAML grammar rules.
*
\***************************************************************************/

// set this flag to turn on whitespace collapse rules.
// #define UseValidatingReader

using System;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Globalization;
using MS.Utility;
using System.Collections.Specialized;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using MS.Internal;

// Disabling 1634 and 1691:
// In order to avoid generating warnings about unknown message numbers and
// unknown pragmas when compiling C# source code with the C# compiler,
// you need to disable warnings 1634 and 1691. (Presharp Documentation)
#pragma warning disable 1634, 1691

#if !PBTCOMPILER

using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;

#endif

#if PBTCOMPILER
namespace MS.Internal.Markup
#else
namespace System.Windows.Markup
#endif
{
    /// <summary>
    /// ContextTypes that can be associated with an XmlElement
    /// </summary>
    internal enum ElementContextType
    {
        Default,             // Anything that is an object instance (CLR or DependencyObject)
        DefTag,              // Code or other x: section
        PropertyComplex,     // Complex Property on any type of object
        PropertyArray,       // Complex Property that is an array
        PropertyIList,       // Complex Property that is an IList
        PropertyIDictionary, // Complex Property that is an IDictionary
        Unknown,             // Tag that that doesn't map to any known class or property
    }


    // define the chars that we need to know about for CSS Tokenization
    internal static class CSSChar
    {
        internal const char Null = '\0';
        internal const char Escape = '\\';
        internal const char Tab = '\t';
        internal const char NewLine = '\n';
        internal const char Return = '\r';
        internal const char FormFeed = '\f';
        internal const char At = '@';
        internal const char Dot = '.';
        internal const char Colon = ':';
        internal const char Single = '\'';
        internal const char Double = '"';
        internal const char Semi = ';';
        internal const char LeftParen = '(';
        internal const char RightParen = ')';
        internal const char LeftCurly = '{';
        internal const char RightCurly = '}';
        internal const char Hash = '#';
        internal const char ForwardSlash = '/';
        internal const char Asterisk = '*';
        internal const char Equal = '=';
        internal const char Underline = '_';
        internal const char Hyphen = '-';
        internal const char Bang = '!';
        internal const char Comma = ',';
        internal const char LeftBracket = '<';
        internal const char RightBracket = '>';
        internal const char A = 'A';
        internal const char a = 'a';
        internal const char Z = 'Z';
        internal const char z = 'z';
        internal const char Zero = '0';
        internal const char Nine = '9';
        internal const char Space = ' ';
        internal const char LeftSquareBracket = '[';
        internal const char RightSquareBracket = ']';
        internal const char Tilde = '~';
        internal const char VLine = '|';
        internal const char Plus = '+';
    }
    
    /// <summary>
    /// XamlReaderHelper class.
    /// </summary>
    internal partial class XamlReaderHelper : IParserHelper
    {
        #region Constructors

        /// <summary>
        /// Constructor. Internal so only the XamlParser and select
        /// Avalon object parsers can call it.
        /// </summary>
        internal XamlReaderHelper(
            XamlParser xamlParser,
            ParserContext parserContext,
            XmlReader xmlReader)
        {

            Debug.Assert(xamlParser != null, "Cannot have null xaml parser");
            Debug.Assert(parserContext != null, "Cannot have null parser context");
            Debug.Assert(xmlReader != null, "Cannot have null xmlReader");

            // use the parser class for making resolution callbacks. for GetElementBaseType
            // probably should break that call + the XamlTypeMapper calls into an interface to
            // comletely abstract the resolution from the tokenizer.
            _xamlParser = xamlParser;
            _parserContext = parserContext;

            XmlReader = xmlReader;
            Normalization = true;

            _xamlNodeCollectionProcessor = new XamlNodeCollectionProcessor();

            // setup the _textFlow stack
            _textFlowStack = new Stack();

            // push a rootLevel stack 
            // For now always use InlineBlock.
            TextFlowStackData textFlowStackData = new TextFlowStackData();
            textFlowStackData.StripLeadingSpaces = true;

            TextFlowStack.Push(textFlowStackData);

            _extensionParser = new MarkupExtensionParser((IParserHelper)this, parserContext);

#if UseValidatingReader
            // turn on to setup validating Reader.
            XmlValidatingReader xmlValidatingReader = new XmlValidatingReader(xmlReader);
            xmlValidatingReader.ValidationType = ValidationType.None;
            XmlReader = xmlValidatingReader;

#endif // UseValidatingReader

        }


        #endregion Constructors

        #region IParserHelper

        string IParserHelper.LookupNamespace(string prefix)
        {
            return XmlReader.LookupNamespace(prefix);
        }

        bool IParserHelper.GetElementType(
                bool extensionFirst,
                string localName,
                string namespaceURI,
            ref string assemblyName,
            ref string typeFullName,
            ref Type baseType,
            ref Type serializerType)
        {
            return GetElementType(
                             extensionFirst,
                             localName,
                             namespaceURI,
                        ref  assemblyName,
                        ref  typeFullName,
                        ref  baseType,
                        ref  serializerType);
        }

        bool IParserHelper.CanResolveLocalAssemblies()
        {
            return ControllingXamlParser.CanResolveLocalAssemblies();
        }

        #endregion IParserHelper

        #region internalMethods

        // methods for callers to Read XamlNodes created by the Tokenizer
        // currently internal since only exposed to the XamlParser and other
        // internal parsers.

        // Return true if the xaml tokenizer should continue
        private bool ContinueReading
        {
            get
            {
                // No more data: stop
                if (!IsMoreData())
                    return false;
                // Created a synthetic clr or element start tag:  continue
                // to see if there is a real one
                if (CurrentContext != null && _readAnotherToken)
                {
                    _readAnotherToken = false;
                    return true;
                }

                // Return true if we need more information to determine whether
                //  the current element will be generated via TypeConverter.
                if( TokenReaderNodeCollection.IsTypeConverterUsageUndecided )
                {
                    return true;
                }

                // Have cached nodes:  stop
                return 0 >= TokenReaderNodeCollection.Count;
            }
        }

        //Used to determine if a type can hold more than one child.
        private static bool IsACollection(Type type)
        {
#if PBTCOMPILER
                return (ReflectionHelper.GetMscorlibType(typeof(IList)).IsAssignableFrom(type)
                        || ReflectionHelper.GetMscorlibType(typeof(IDictionary)).IsAssignableFrom(type)
                        || ReflectionHelper.GetMscorlibType(typeof(Array)).IsAssignableFrom(type));
#else
                return (typeof(IList).IsAssignableFrom(type)
                        || typeof(IDictionary).IsAssignableFrom(type)
                        || typeof(Array).IsAssignableFrom(type));
#endif
        }

        //Used to determine if a container can hold more than one child.
        private static bool IsACollection(ElementContextStackData context)
        {
            //If there is a CPA, check if the property's type is a collection
            if (context.IsContentPropertySet)
            {
                Type contentPropertyType = XamlTypeMapper.GetPropertyType(context.ContentPropertyInfo);
                return IsACollection(contentPropertyType);
            }
            else
            //otherwise, look at the contexttype and decide with that data.
            {
                ElementContextType contextType = context.ContextType;
                switch (contextType)
                {

                    //These can all hold multiple items
                    case ElementContextType.PropertyIList:
                    case ElementContextType.PropertyIDictionary:
                    case ElementContextType.PropertyArray:

                    //In markup compile pass 1, we need to pretend that it is a collection...since it may be.
                    //If it is not a collection, when pass 2 happens, the right answer will be provided.
                    case ElementContextType.Unknown:
                        return true;

                    //Otherwise, look at the contextData's type to see if it is a collection or supports IAddChildInternal
                    case ElementContextType.Default:
                    case ElementContextType.PropertyComplex:
                        Type t = ((Type)context.ContextData);
                        if (IsACollection(t) || BamlRecordManager.TreatAsIAddChild(t))
                            return true;
                        break;
                }
                return false;
            }
        }

#if !PBTCOMPILER
        /// <summary>
        /// Answer the encoding of the underlying xaml stream
        /// </summary>
        internal System.Text.Encoding Encoding
        {
            get
            {
                XmlCompatibilityReader xmlCompatReader = _xmlReader as XmlCompatibilityReader;

                if (xmlCompatReader != null)
                {
                    return xmlCompatReader.Encoding;
                }
                else
                {
                    XmlTextReader textReader = _xmlReader as XmlTextReader;
                    if (textReader != null)
                    {
                        return textReader.Encoding;
                    }
                }

                //Can't tell encoding from underlying stream, assume UTF8
                return new System.Text.UTF8Encoding(true, true);
            }
        }
#endif

        /// <summary>
        /// Close the reader so that the underlying stream and / or file is closed.
        /// </summary>
        internal void Close()
        {
            if (_xmlReader != null)
            {
                _xmlReader.Close();
                _xmlReader = null;
            }
        }

        /// <summary>
        /// Reads the next XamlNode
        /// </summary>
        /// <param name="xamlNode">pointer to the XamlNode Read</param>
        /// <returns>true if a Nodes was read, False if no more nodes.</returns>
        internal bool Read(ref XamlNode xamlNode)
        {
            xamlNode = null;

            // see if there is a node in the buffer and if so just return it
            if (TokenReaderNodeCollection.Count > 0)
            {
                xamlNode = TokenReaderNodeCollection.Remove();
                Debug.Assert(null != xamlNode, "null returned from the collection");
                return true;
            }

            if (CurrentContext != null && CurrentContext.IsEmptyElement)
            {
                // if the current element on the stack is an empty element,
                // we need pop the stack here as all its attributes would
                // have already been processed by the XamlParser.
                ElementContextStack.Pop();
                ParserContext.PopScope();
            }

            // the the parseLoop is done, just return false
            if (ParseLoopState == ParserState.Done)
            {
                xamlNode = null;
                return false;
            }

#if PBTCOMPILER || !STRESS
            try // What do we do with Exceptions we catch on this thread?.
            {
#endif

                // Do any processing specific to the
                // first time Read is called
                if (ParseLoopState == ParserState.Uninitialized)
                {
                    Debug.Assert(null != XmlReader);
                    ParseLoopState = ParserState.Reading;

                    WriteDocumentStart(); // write the document node.

                    XmlReader.Read(); // first time thru get to the first start tag.
                }

                // loop calling the appropriate context handler
                // !! when calling we pass in a NodeType which should be used
                // to determine the nodeState. This is necessary for the case of an
                // empty tag.
                // keep reading while there is no data and we don't have
                // any items in the Node collection to return.
                while (ContinueReading)
                {

                    // read based on the node type.
                    switch (XmlReader.NodeType)
                    {
                        case XmlNodeType.Element:
                            ReadElementNode();
                            break;
                        case XmlNodeType.EndElement:
                            ReadEndElementNode(false);
                            break;
                        default:
                            ReadGenericXmlNode();
                            break;
                    }

                }

                // If there aren't any nodes in the buffer when come out of
                // loop means we are done with the XML, so set the state
                // to done and write the EndDocument.

                if (0 >= TokenReaderNodeCollection.Count && ParseLoopState != ParserState.Done)
                {
                    Debug.Assert(!IsMoreData(), "Setting state to done but not done parsing.");
                    ParseLoopState = ParserState.Done;
                    WriteDocumentEnd();
                }
#if PBTCOMPILER || !STRESS
            }
            catch (XamlParseException e)
            {
                throw e;
            }
            catch (XmlException e)
            {
                // if it is an XML Exception use the lineNumber/position they tell us.
                RethrowAsParseException(e.Message, e.LineNumber, e.LinePosition, e);
            }
            catch (Exception e)
            {
                if (CriticalExceptions.IsCriticalException(e))
                {
                    throw;
                }
                else
                {
                    // generic exception, use current linenumber, position form XmlReader.
                    RethrowAsParseException(e.Message, LineNumber, LinePosition, e);
                }
            }
#endif

            // see if there is a node in the buffer and if so return it.
            if (TokenReaderNodeCollection.Count > 0)
            {
                xamlNode = TokenReaderNodeCollection.Remove();
                Debug.Assert(null != xamlNode, "null returned from the collection");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Used to determined whether the reader is within the context of an Xml data island.
        /// </summary>
        /// <returns>
        /// true if within the context of an Xml Data Island
        /// </returns>
        internal bool IsXmlDataIsland()
        {
            return (_xmlDataIslandDepth != -1);
        }
        #endregion internalMethods

        #region CacheCallbacks

        /// <summary>
        /// Resolves an Assembly and TypeFullName to an ElementType.  Note that the compiler
        /// uses this callback to grep the defined class name out of the attributes, so even
        /// though the token reader doesn't need to callout to the parser, do so anyway so the
        /// compiler won't die.
        /// </summary>
        bool GetElementType(
                bool extensionFirst,  // True if we check for localName+Extension first
                string localName,
                string namespaceURI,
            ref string assemblyName,
            ref string typeFullName,
            ref Type baseType,
            ref Type serializerType)
        {
            if (extensionFirst)
            {
                if (ControllingXamlParser.GetElementType(XmlReader, localName + "Extension",
                               namespaceURI, ref assemblyName, ref typeFullName,
                               ref baseType, ref serializerType))
                {
                    return true;
                }
                else
                {
                    return ControllingXamlParser.GetElementType(XmlReader, localName,
                               namespaceURI, ref assemblyName, ref typeFullName,
                               ref baseType, ref serializerType);
                }
            }
            else
            {
                if (ControllingXamlParser.GetElementType(XmlReader, localName,
                                namespaceURI, ref assemblyName, ref typeFullName,
                                ref baseType, ref serializerType))
                {
                    return true;
                }
                else
                {
                    return ControllingXamlParser.GetElementType(XmlReader, localName + "Extension",
                               namespaceURI, ref assemblyName, ref typeFullName,
                               ref baseType, ref serializerType);
                }
            }
        }



#if !PBTCOMPILER
        /// <summary>
        /// Get the parse mode from the XamlParser, if present.  Otherwise default to sync
        /// </summary>
        internal XamlParseMode XamlParseMode
        {
            get
            {
                return ControllingXamlParser.XamlParseMode;
            }
        }
#endif

        #endregion CacheCallbacks

        #region RecordWriters

        // Helpers called whenver the TokenReader wants to add a NodeType. packages up a XamlNode
        // and then Calls AddNodeToCollection to add it to the collection.

        /// <summary>
        /// Write a document node.
        /// </summary>
        void WriteDocumentStart()
        {
            AddNodeToCollection(new XamlDocumentStartNode(LineNumber, LinePosition, 0));
        }

        /// <summary>
        /// Write an EndDocument node.
        /// </summary>
        void WriteDocumentEnd()
        {
            AddNodeToCollection(new XamlDocumentEndNode(LineNumber, LinePosition, XmlReader.Depth));
        }

        /// <summary>
        /// Write a Unknown xaml node start record
        /// </summary>
        void WriteUnknownTagStart(
            string namespaceUri,
            string tagName,
            int depth)
        {
            AddNodeToCollection(new XamlUnknownTagStartNode(LineNumber, LinePosition,
                                                          depth, namespaceUri, tagName));
        }

        /// <summary>
        /// Write a Unknown xaml attribute record
        /// </summary>
        void WriteUnknownAttribute(
            string namespaceUri,
            string attributeName,
            string attributeValue,
            int depth,
            string parentTypeNamespace,
            string parentTypeName,
            object dynamicObject,
            HybridDictionary resolvedProperties)
        {
#if PBTCOMPILER
            bool localAssembly = false;
            string ownerTypeFullName = string.Empty;

            if (parentTypeName != null)
            {
                NamespaceMapEntry[] namespaceMaps = XamlTypeMapper.GetNamespaceMapEntries(parentTypeNamespace);

                localAssembly = (namespaceMaps != null && namespaceMaps.Length == 1 && namespaceMaps[0].LocalAssembly);
                if (localAssembly)
                {
                    ownerTypeFullName = namespaceMaps[0].ClrNamespace + "." + parentTypeName;
                }
            }
#endif

            string namePropertyName = GetRuntimeNamePropertyName(parentTypeName, parentTypeNamespace);

            BamlAttributeUsage attributeUsage = BamlAttributeUsage.Default;
            if (IsNameProperty(attributeValue, parentTypeName, attributeName, namespaceUri, namePropertyName))
            {
                CheckDuplicateProperty(resolvedProperties, attributeName, dynamicObject);
            }

            XamlUnknownAttributeNode xamlUnknownAttributeNode = new XamlUnknownAttributeNode(LineNumber, LinePosition, depth,
                                                  namespaceUri, attributeName, attributeValue, attributeUsage);

#if PBTCOMPILER
            xamlUnknownAttributeNode.OwnerTypeFullName = ownerTypeFullName;
#endif
            AddNodeToCollection(xamlUnknownAttributeNode);
        }

        /// <summary>
        /// Write a Unknown xaml node end record
        /// </summary>
        void WriteUnknownTagEnd()
        {
            // We pass on the local name and namespace uri because it is used to distinguish
            // between x:Array end tag and Set.Value end tags
            UnknownData data = CurrentContext.ContextData as UnknownData;
            AddNodeToCollection(new XamlUnknownTagEndNode(LineNumber, LinePosition, XmlReader.Depth,
                data.LocalName, data.NamespaceURI));
        }

        /// <summary>
        /// Write a Start Element node, which can be any type of object
        /// </summary>
        void WriteElementStart(
            string assemblyName,
            string typeFullName,
            int depth,
            Type elementType,
            Type serializerType,
            bool isInjected)
        {
            // This element needs a dictionary key if we are directly under an IDictionary
            // property or an IDictionary object
            bool needsKey =
                   ParentContext != null &&
                   (ParentContext.ContextType == ElementContextType.PropertyIDictionary ||
#if PBTCOMPILER
                    ReflectionHelper.GetMscorlibType(typeof(IDictionary)).IsAssignableFrom(ParentContext.ContextData as Type));
#else
                    typeof(IDictionary).IsAssignableFrom(ParentContext.ContextData as Type));
#endif
            AddNodeToCollection(new XamlElementStartNode(XamlNodeType.ElementStart, LineNumber, LinePosition, depth, assemblyName,
                            typeFullName, elementType, serializerType, XmlReader.IsEmptyElement,
                            needsKey, isInjected));
        }

        /// <summary>
        /// Write an End Element node
        /// </summary>
        internal void WriteElementEnd()
        {
            int depth = XmlReader.Depth;
            if (CurrentContext.IsEmptyElement)
            {
                // when injecting an ElementEnd node for an empty element, its
                // depth should match the ElementStart node.  But the XmlReader's
                // depth has been incremented.
                depth -= 1;
            }

            AddNodeToCollection(new XamlElementEndNode(LineNumber, LinePosition, depth));

            //If this element had any content, check for a duplicate setting of that property.
            if (CurrentContext.ContentParserState != ParsingContent.Before)
            {
                if (CurrentContext.IsContentPropertySet)
                {
                    PropertyInfo contentPropertyInfo = CurrentContext.ContentPropertyInfo;
                    CheckDuplicateProperty(CurrentProperties, XamlTypeMapper.GetPropertyName(contentPropertyInfo), contentPropertyInfo);
                }
                else
                {
                    // ParsingContent is advanced but we have no Content Property Set....?
                    // This must mean IAddChild was used.   We want to check that against
                    // explicit duplicate use of the Content Property.
                    Type elementType = (Type) CurrentContext.ContextDataType;
                    string namespaceUri = CurrentContext.NamespaceUri;

                    string contentPropertyName = GetContentPropertyName(elementType);
                    if (null != contentPropertyName)
                    {
                        string propertyAssemblyName;
                        object propertyDynamicObject;
                        Type propertyDeclaringType;

                        ResolveContentProperty(contentPropertyName, elementType, namespaceUri,
                                               out propertyAssemblyName, out propertyDynamicObject, out propertyDeclaringType);

                        PropertyInfo contentPropertyInfo = (PropertyInfo)propertyDynamicObject;
                        CheckDuplicateProperty(CurrentProperties, XamlTypeMapper.GetPropertyName(contentPropertyInfo), contentPropertyInfo);
                    }
                }
            }
        }

        /// <summary>
        /// write out a text node.
        /// </summary>
        void WriteText(
            string value,
            Type converterType,
            int depth)
        {
            AddNodeToCollection(new XamlTextNode(LineNumber, LinePosition, depth, value, converterType));
        }

        /// <summary>
        /// Write out the start of a PropertyComplex node
        /// </summary>
        /// <remarks>
        /// Determining whether a tag is a complex property can require advancing the reader
        /// to the next tag in the file.  To get accurate positioning information, use
        /// the passed in lineNumber and linePosition rather than the XmlReader's current location.
        /// </remarks>
        void WritePropertyComplexStart(
            int depth,
            int lineNumber,
            int linePosition,
            object propertyMember,          // DependencyProperty, MethodInfo or PropertyInfo
            string declaringAssemblyName,   // Assembly of declaring type or owner of the CLR property
            string declaringTypeFullName,   // Full name of the type where the property is declared
            string propIdName,
            HybridDictionary properties)           // Property collection that contains all complex props
        {
            CheckDuplicateProperty(properties, propIdName, propertyMember);
            AddNodeToCollection(new XamlPropertyComplexStartNode(lineNumber, linePosition, depth,
                        propertyMember, declaringAssemblyName, declaringTypeFullName, propIdName));

        }

        /// <summary>
        /// Write out the End of a PropertyComplex node
        /// </summary>
        void WritePropertyComplexEnd()
        {
            AddNodeToCollection(new
                XamlPropertyComplexEndNode(LineNumber, LinePosition, XmlReader.Depth));
        }

        /// <summary>
        /// Write out literal content node
        /// </summary>
        internal void WriteLiteralContent(
            string textValue,
            int depth,
            int lineNumber,
            int linePosition)
        {
            AddNodeToCollection(
                    new XamlLiteralContentNode(lineNumber, linePosition, depth, textValue));

        }

        void WriteNameProperty(
            string propertyName,            // String name of the property in xaml markup
            object propertyMember,          // DependencyProperty, PropertyInfo or MethodInfo for static setter
            string assemblyName,            // Assembly of the type where the property is declared
            string declaringTypeFullName,   // Full name of the type where the property is declared
            string value,                   // String value of the property
            BamlAttributeUsage usage)       // Defines special usage for this property, such as xml:lang
        {
            AddNodeToCollection(new XamlPropertyNode(
                LineNumber, LinePosition, XmlReader.Depth, propertyMember,
                assemblyName, declaringTypeFullName, propertyName, value, usage, false, true), true, true);
        }

        /// <summary>
        /// Write out a property that is represented as an attribute on a tag.
        /// </summary>
        void WriteProperty(
            string propertyName,            // String name of the property in xaml markup
            object propertyMember,          // DependencyProperty, PropertyInfo or MethodInfo for static setter
            string assemblyName,            // Assembly of the type where the property is declared
            string declaringTypeFullName,   // Full name of the type where the property is declared
            string value,                   // String value of the property
            BamlAttributeUsage usage)       // Defines special usage for this property, such as xml:lang
        {
            bool isName = usage == BamlAttributeUsage.RuntimeName;
            AddNodeToCollection(new XamlPropertyNode(
                LineNumber, LinePosition, XmlReader.Depth, propertyMember,
                assemblyName, declaringTypeFullName, propertyName, value, usage, false), isName, isName);
        }

        /// <summary>
        /// Write out a property whose value is a simple MarkupExtension.
        /// </summary>
        void WritePropertyWithExtension(
            string propertyName,            // String name of the property in xaml markup
            object propertyMember,          // DependencyProperty, PropertyInfo or MethodInfo for static setter
            string assemblyName,            // Assembly of the type where the property is declared
            string declaringTypeFullName,   // Full name of the type where the property is declared
            string value,                   // String value of the property
            short extensionTypeId,
            bool isValueNestedExtension,
            bool isValueTypeExtension)
        {
            MarkupExtensionParser.RemoveEscapes(ref value);

            if (extensionTypeId == (short)KnownElements.TypeExtension)
            {
                string typeValueFullName = value;  // set this to original value for error reporting if reqd.
                string typeValueAssemblyFullName = null;
                Type typeValue = XamlTypeMapper.GetTypeFromBaseString(value,
                                                                      ParserContext,
                                                                      true);
                // if the type is locally defined, GetTypeFromBaseString() will return null in pass1,
                // so that we can continue on. In pass2, if the type is still not found, this will
                // still be null, but this will be checked for in BamlRecordWriter.WritePropertyWithExtension
                // where an appropriate exception will be thrown. We can't throw here as the XamlReader
                // has no idea about passes of compilation.
                if (typeValue != null)
                {
                    typeValueFullName = typeValue.FullName;
                    typeValueAssemblyFullName = typeValue.Assembly.FullName;
                }

                WritePropertyWithType(propertyName,
                                      propertyMember,
                                      assemblyName,
                                      declaringTypeFullName,
                                      typeValueFullName,
                                      typeValueAssemblyFullName,
                                      typeValue,
                                      string.Empty,
                                      string.Empty);
            }
            else
            {
                AddNodeToCollection(new XamlPropertyWithExtensionNode(LineNumber,
                                                                      LinePosition,
                                                                      XmlReader.Depth,
                                                                      propertyMember,
                                                                      assemblyName,
                                                                      declaringTypeFullName,
                                                                      propertyName,
                                                                      value,
                                                                      extensionTypeId,
                                                                      isValueNestedExtension,
                                                                      isValueTypeExtension));
            }
        }

        /// <summary>
        /// Write out a property that is represented as an attribute on a tag.
        /// </summary>
        void WritePropertyWithType(
            string propertyName,            // String name of the property in xaml markup
            object propertyMember,          // DependencyProperty, PropertyInfo or MethodInfo for static setter
            string assemblyName,            // Assembly where type of the CLR property is defined
            string declaringTypeFullName,   // Full name of the type where the property is declared
            string valueTypeFullname,       // String value of type, in the form Namespace.Typename
            string valueAssemblyName,       // Assembly name where type value is defined.
            Type valueElementType,        // Actual type of the valueTypeFullname.
            string valueSerializerTypeFullName,     // Name of serializer to use for valueElementType, if present
            string valueSerializerTypeAssemblyName) // Name of assembly that holds the serializer
        {
            AddNodeToCollection(new XamlPropertyWithTypeNode(
                LineNumber, LinePosition, XmlReader.Depth, propertyMember,
                assemblyName, declaringTypeFullName, propertyName, valueTypeFullname,
                valueAssemblyName, valueElementType, valueSerializerTypeFullName,
                valueSerializerTypeAssemblyName));
        }

        /// <summary>
        /// Write out a property that is represented as a complex property as if it were
        /// a regular attribute.
        /// </summary>
        void WriteComplexAsSimpleProperty(
            string propertyName,            // String name of the property in xaml markup
            string propertyNamespaceUri,    // Namespace corresponding to the property name
            object propertyMember,          // DependencyProperty, PropertyInfo or MethodInfo for static setter
            string assemblyName,            // Assembly where type of the CLR property is defined
            Type declaringType,           // Type that corresponds to declaringTypeFullName (where the property is declared)
            string declaringTypeFullName,   // Full name of the type where the property is declared
            string value,                   // String value of the property
            BamlAttributeUsage usage)       // Defines special usage for this property, such as xml:lang
        {
            CheckDuplicateProperty(ParentProperties, propertyName, propertyMember);

            // Validate that enum values aren't pure digits.
            Type propType = XamlTypeMapper.GetPropertyType(propertyMember);
            XamlTypeMapper.ValidateEnums(propertyName, propType, value);

            // See if this is the property that maps to x:Name, and if so, set the usage flag.
            string parentName = declaringType != null ? declaringType.Name : string.Empty;
            if (IsNameProperty(value, parentName, propertyName, propertyNamespaceUri, GetRuntimeNamePropertyName(declaringType)))
            {
                usage = BamlAttributeUsage.RuntimeName;
            }


            AddNodeToCollection(new XamlPropertyNode(
                LineNumber, LinePosition, XmlReader.Depth, propertyMember,
                assemblyName, declaringTypeFullName, propertyName, value, usage, true));
        }

        void WriteContentProperty(
            int depth,
            int lineNumber,
            int linePosition,
            object propertyMember,          // DependencyProperty, MethodInfo or PropertyInfo
            string declaringAssemblyName,   // Assembly of declaring type or owner of the CLR property
            string declaringTypeFullName,   // Full name of the type where the property is declared
            string propertyName)            // Name of the content property
        {
            AddNodeToCollection(new XamlContentPropertyNode(lineNumber, linePosition, depth,
                propertyMember, declaringAssemblyName, declaringTypeFullName, propertyName));
        }

#if !PBTCOMPILER
        /// <summary>
        /// Write out a RoutedEvent node.
        /// </summary>
        void WriteRoutedEvent(
            RoutedEvent routedEvent,
            string assemblyName,
            string typeFullName,
            string eventName,
            string value)
        {
            AddNodeToCollection(new XamlRoutedEventNode(LineNumber, LinePosition, XmlReader.Depth, routedEvent,
                    assemblyName, typeFullName, eventName, value));

        }
#endif

        /// <summary>
        /// Write out a NamespacePrefix node.
        /// </summary>
        void WriteNamespacePrefix(string prefix, string namespaceUri)
        {
            AddNodeToCollection(new XamlXmlnsPropertyNode(LineNumber, LinePosition, XmlReader.Depth,
                        prefix, namespaceUri));
        }

        /// <summary>
        /// Write out mapping instruction
        /// </summary>
        void WritePI(string xmlnsValue, string clrnsValue, string assyValue)
        {
            AddNodeToCollection(new XamlPIMappingNode(LineNumber, LinePosition, XmlReader.Depth,
                xmlnsValue, clrnsValue, assyValue));
        }

        /// <summary>
        /// Write out a ClrEvent Node
        /// </summary>
        void WriteClrEvent(string eventName, MemberInfo eventMember, string value)
        {
            CheckDuplicateProperty(CurrentProperties, eventName, eventMember);
            AddNodeToCollection(new XamlClrEventNode(LineNumber, LinePosition, XmlReader.Depth,
                    eventName, eventMember, value), true, false);
        }

        /// <summary>
        ///  Write out an Array start Property Node
        /// </summary>
        void WritePropertyArrayStart(
            int depth,
            object propertyMember,              // DependencyProperty, MethodInfo or PropertyInfo
            string declaringAssemblyName,   // Assembly of declaring type or owner of the CLR property
            string declaringTypeFullName,   // Full name of the type where the property is declared
            string propIdName)
        {
            CheckDuplicateProperty(ParentProperties, propIdName, propertyMember);
            AddNodeToCollection(new XamlPropertyArrayStartNode(LineNumber, LinePosition, depth,
                        propertyMember, declaringAssemblyName, declaringTypeFullName, propIdName));
        }

        /// <summary>
        /// Write out an End Array Property node
        /// </summary>
        void WritePropertyArrayEnd()
        {
            AddNodeToCollection(
                new XamlPropertyArrayEndNode(LineNumber, LinePosition, XmlReader.Depth));
        }

        /// <summary>
        ///  Write out a start IList Property Node
        /// </summary>
        void WritePropertyIListStart(
                int depth,
                object propertyMember,              // DependencyProperty, MethodInfo or PropertyInfo
                string declaringAssemblyName,   // Assembly of declaring type or owner of the CLR property
                string declaringTypeFullName,   // Full name of the type where the property is declared
                string propIdName)
        {
            CheckDuplicateProperty(ParentProperties, propIdName, propertyMember);
            AddNodeToCollection(new XamlPropertyIListStartNode(LineNumber, LinePosition, depth,
                        propertyMember, declaringAssemblyName, declaringTypeFullName, propIdName));

        }

        /// <summary>
        /// Write out an End IList Property node
        /// </summary>
        void WritePropertyIListEnd()
        {
            AddNodeToCollection(
                new XamlPropertyIListEndNode(LineNumber, LinePosition, XmlReader.Depth));
        }

        /// <summary>
        ///  Write out a start IDictionary Property Node
        /// </summary>
        void WritePropertyIDictionaryStart(
                int depth,
                object propertyMember,              // DependencyProperty, MethodInfo or PropertyInfo
                string declaringAssemblyName,   // Assembly of declaring type or owner of the CLR property
                string declaringTypeFullName,   // Full name of the type where the property is declared
                string propIdName)
        {
            CheckDuplicateProperty(ParentProperties, propIdName, propertyMember);
            AddNodeToCollection(new XamlPropertyIDictionaryStartNode(LineNumber, LinePosition, depth,
                        propertyMember, declaringAssemblyName, declaringTypeFullName, propIdName));

        }

        /// <summary>
        /// Write out an End IDictionary roperty node
        /// </summary>
        void WritePropertyIDictionaryEnd()
        {
            AddNodeToCollection(
                new XamlPropertyIDictionaryEndNode(LineNumber, LinePosition, XmlReader.Depth));
        }

        /// <summary>
        /// Write out an EndAttributes Node.
        /// </summary>
        void WriteEndAttributes(int depth, bool compact)
        {
            AddNodeToCollection(new XamlEndAttributesNode(LineNumber, LinePosition, depth, compact));
        }

        /// <summary>
        /// Write out a DefTag.
        /// </summary>
        void WriteDefTag(string defTagName)
        {
            //!!!Review. This passes out the XmlReader for the compiler to process the
            // def tags. Should package these into records for validation and so
            // don't have to hand out the XmlReader.
            AddNodeToCollection(
                new XamlDefTagNode(LineNumber, LinePosition, XmlReader.Depth,
                                    XmlReader.IsEmptyElement, XmlReader, defTagName));
        }


        /// <summary>
        /// Write out an attirbute in the definition namespace
        /// </summary>
        void WriteDefAttribute(string name, string value)
        {
            WriteDefAttribute(name, value, BamlAttributeUsage.Default);
        }

        /// <summary>
        /// Write out an attribute in the definition namespace
        /// </summary>
        void WriteDefAttribute(string name, string value, BamlAttributeUsage bamlAttributeUsage)
        {
            AddNodeToCollection(new XamlDefAttributeNode(LineNumber, LinePosition, XmlReader.Depth,
                    name, value, bamlAttributeUsage));
        }

        /// <summary>
        /// Write out an attribute in the PresentationOptions namespace
        /// </summary>
        void WritePresentationOptionsAttribute(string name, string value)
        {
            AddNodeToCollection(new XamlPresentationOptionsAttributeNode(LineNumber, LinePosition, XmlReader.Depth,
                    name, value));
        }

        /// <summary>
        /// Write out a key attirbute in the definition namespace whose value is a Type
        /// object.  This is a common case for x:Key="{x:Type SomeType}"
        /// </summary>
        void WriteDefKeyWithType(
            string valueTypeFullName,
            string valueAssemblyName,
            Type valueElementType)
        {
            AddNodeToCollection(new XamlDefAttributeKeyTypeNode(LineNumber, LinePosition, XmlReader.Depth,
                 valueTypeFullName, valueAssemblyName, valueElementType));
        }

        /// <summary>
        /// Check if this property is a duplicate of one that has already been
        /// parsed for the parent DependencyObject.  If so, throw an error
        /// </summary>
        void CheckDuplicateProperty(
            HybridDictionary properties,
            string propertyName,
            object propertyMember)
        {
            if (propertyMember != null)
            {
                int dotIndex = propertyName.LastIndexOf('.');
                if (-1 != dotIndex)
                {
                    propertyName = propertyName.Substring(dotIndex+1);
                }
                // MemberInfo is a base class of PropertyInfo.
                MemberInfo mi = propertyMember as MemberInfo;
                XamlPropertyFullName declaringProp = new XamlPropertyFullName(mi.DeclaringType, propertyName);
                XamlPropertyFullName usageProp = new XamlPropertyFullName(mi.ReflectedType, propertyName);

                if (properties.Contains(declaringProp))
                {
                    XamlPropertyFullName oldUsageProp = (XamlPropertyFullName)properties[declaringProp];
                    if (usageProp.Equals(oldUsageProp))
                    {
                        ThrowException(SRID.ParserDuplicateProperty1, usageProp.FullName);
                    }
                    else
                    {
                        ThrowException(SRID.ParserDuplicateProperty2,
                                    usageProp.FullName, oldUsageProp.FullName);
                    }
                }
                else
                {
                    properties[declaringProp] = usageProp;
                }
            }
        }


        #endregion // RecordWriters

        #region ResolutionHelpers

        // private helper methods for resolving tags and attributes to element, clrObject, etc.

        /// <summary>
        /// Given a localName and a namespaceURI from a tag determines if it
        /// resolves to a DependencyProperty or ClrProperty depending on the parentContext.
        /// Returns true if the tag resolved to a complex object.
        /// </summary>
        private bool GetPropertyComplex(
                string ownerName,
                string localName,
                string namespaceURI,
            ref string assemblyName,      // Assembly name of the owner of the dynamicObject
            ref string typeFullName,      // typeFullName of the object that the dynamicObject is on
            ref string dynamicObjectName, // Property name withoout decorations (ie - no "Set" prefix)
            ref Type baseType,       // Type of the dynamicObject - PropertyInfo, MethodInfo or DependencyProperty
            ref Object dynamicObject,     // Property found -> PI, MI or DP
            ref Type declaringType)       // Actual type corresponding to typeFullName
        {

            baseType = null;

            // If we don't have a ParentContext then we can't resolve the complex property.
            if (null == ParentContext)
                return false;

            ElementContextType parentType = ParentContext.ContextType;


            // Determine if we can have a complex property given the context
            if (ElementContextType.Default != parentType &&
                ElementContextType.Unknown != parentType )
            {
                return false;
            }

            // Determine the type to lookup and if valid see if it
            // can resolve to an Attribute Context.  Look for
            // normal clr properties first and if this doesn't work,
            // try to resolve this as an attached DependencyProperty.
            AttributeContext attribContext = GetDottedAttributeContext(
                                                ParentContext.ContextDataType,
                                                namespaceURI,
                                                namespaceURI,
                                                ownerName,
                                                localName,
                                            ref dynamicObject,
                                            ref assemblyName,
                                            ref typeFullName,
                                            ref declaringType,
                                            ref dynamicObjectName);

            if (AttributeContext.Unknown == attribContext || dynamicObject == null)
            {
                return false;
            }
            Debug.Assert(null != typeFullName, "type name not set");
            Debug.Assert(null != dynamicObjectName, "dynamicObject name not set");
            Debug.Assert(null != assemblyName, "assembly name not set");

            // If found an Attribute Context then setup the baseType.
            baseType = dynamicObject.GetType();
            return true;
        }


        /// <summary>
        /// Determines if the contents of a complex property declaration is truly
        /// complex or contains just simple text.
        /// This loops through XML until it hits an end tag or a non-text value.
        /// <para/>
        /// !!!Review - if we should change this so goes ahead an always make a
        /// complex property definition but in the case clrObject ends up just having Text
        /// we write out a clrObject record indicating it just has simple content
        /// so this loop can be removed
        /// </summary>
        /// <param name="isEmptyElement">lets Determine know if it is on an empty element </param>
        /// <param name="textValue">returns any text read in the process</param>
        /// <param name="endTagReached">set to true if the end tag of this complex property has
        ///  been read </param>
        /// <returns>true if this is the start of an object declaration or false
        /// if only text was found</returns>
        bool  DetermineIfPropertyComplex(
                bool      isEmptyElement,
            out string    textValue,
            out bool      endTagReached)
        {
            textValue = null;
            endTagReached = false;
            bool propertyComplex = false;

            bool xmlSpacePreserveSet = false;
            bool stripAllLeadingSpaces = true;
            bool lastTextWasPreserved = false; // flag to know how to collapse final trailing space.
            StringBuilder textValueStringBuilder = new StringBuilder();

            if (isEmptyElement)
            {
                // An Empty Element is not allowed if this
                // looks at all like a complex property, so complain
                ThrowException(SRID.ParserEmptyComplexProp, XmlReader.Name);
            }

            // see if XmlSpace is set in the context for whitespace
            // collapse so we don't have to lookup each time.
            if (null != ParserContext.XmlSpace &&
                ParserContext.XmlSpace.Equals("preserve"))
            {
                xmlSpacePreserveSet = true;
            }

            // loop until hit the end Element or another Element tag.
            bool readNextNode = true;
            while (readNextNode && XmlReader.Read())
            {
                switch (XmlReader.NodeType)
                {
                    case XmlNodeType.SignificantWhitespace:
                    case XmlNodeType.Text:
                    case XmlNodeType.CDATA:
                    case XmlNodeType.Whitespace:

                        // collapse based on preserve and if last person
                        // ended with the whitespace.

                        lastTextWasPreserved = (xmlSpacePreserveSet) ? true : false;

                        string collapsedText = CollapseText(XmlReader.Value,
                            stripAllLeadingSpaces /* stripAllLeadingSpaces */,
                            false /* stripAllRightWhitespace */ ,
                            lastTextWasPreserved /* preserve */,
                            out  stripAllLeadingSpaces); // set stripAllLeading spaces based on result.

                        textValueStringBuilder.Append(collapsedText);

                        break;

                    case XmlNodeType.Element:
                        // If we get an element tag, and we've accumulated some non-whitespace text,
                        // then this is an error since you can't have text and an element under
                        // a complex property declaration.  Note that we have to check the contents
                        // of the string builder for whitespace, since it may contain whitespace
                        // due to the LayoutType of the surrounding element, or xmlspace:preserve
                        for (int i = 0; i < textValueStringBuilder.Length; i++)
                        {
                            if (!IsWhiteSpace(textValueStringBuilder[i]))
                            {
                                ThrowException(SRID.ParserTextInComplexProp,
                                               textValueStringBuilder.ToString(),
                                               XmlReader.LocalName);
                            }
                        }
                        propertyComplex = true;
                        readNextNode = false;
                        break;

                    case XmlNodeType.EndElement:
                        // if the last whitespace wasn't preserved then if the textValue ends with a space
                        // strip it

                        if (!lastTextWasPreserved && textValueStringBuilder.Length > 0)
                        {
                            char lastChar = textValueStringBuilder[textValueStringBuilder.Length - 1];

                            if (IsWhiteSpace(lastChar))
                            {
                                textValueStringBuilder.Remove(textValueStringBuilder.Length - 1, 1);
                            }
                        }

                        if (textValueStringBuilder.Length > 0)
                        {
                            textValue = textValueStringBuilder.ToString();
                        }

                        propertyComplex = false;
                        readNextNode = false;
                        endTagReached = true;

                        // if endElement read past it since we dont'want it to show up
                        // in the next read.
                        XmlReader.Read();
                        break;

                    case XmlNodeType.Attribute: // review this.
                        propertyComplex = true;
                        readNextNode = false;
                        break;

                    case XmlNodeType.Comment:
                        break; // keep going on whitespace an comments.

                    case XmlNodeType.ProcessingInstruction:
                        CompilePI();
                        break;

                    default:
                        ThrowException(SRID.ParserUnknownXmlType,
                                       XmlReader.NodeType.ToString());
                        break;
                }
            }

            return propertyComplex;
        }

        /// <summary>
        ///  Helper function for  use to find out the whitespace trimming
        ///  associated with a Type.
        /// </summary>
        bool GetTrimSurroundingWhitespace(Type type)
        {
            // use the XamlTypeMapper cache.
            return XamlTypeMapper.GetCachedTrimSurroundingWhitespace(type);
        }


        #endregion // ResolutionHelpers

        #region Exceptions

        /// <summary>
        /// helper method called to throw an exception.
        /// Wraps the message with the current lineNumber and Postion
        /// then throws a XamlParseException.
        /// </summary>
        void ThrowException(string id)
        {
            string message = SR.Get(id);
            ThrowExceptionWithLine(message);
        }


        void ThrowException(string id, string parameter)
        {
            string message = SR.Get(id, parameter);
            ThrowExceptionWithLine(message);
        }

        void ThrowException(string id, string parameter1, string parameter2)
        {
            string message = SR.Get(id, parameter1, parameter2);
            ThrowExceptionWithLine(message);
        }

        void ThrowException(string id, string parameter1, string parameter2, string parameter3)
        {
            string message = SR.Get(id, parameter1, parameter2, parameter3);
            ThrowExceptionWithLine(message);
        }

        void RethrowAsParseException(
            string keyString,
            int lineNumber,
            int linePosition,
            Exception innerException)
        {
            string messageWithLineNumber = keyString;

            // Xml exceptions already have line and position, so we don't have to add it again.
            if (innerException != null &&
                !typeof(System.Xml.XmlException).IsAssignableFrom(innerException.GetType()))
            {
                messageWithLineNumber += " ";
                messageWithLineNumber += SR.Get(SRID.ParserLineAndOffset,
                                                lineNumber.ToString(CultureInfo.CurrentCulture),
                                                linePosition.ToString(CultureInfo.CurrentCulture));
            }

            XamlParseException parseException;

            // If the exception was a XamlParse exception on the other
            // side of a Reflection Invoke, then just pull up the Parse exception.
            if (innerException is TargetInvocationException && innerException.InnerException is XamlParseException)
            {
                  parseException = innerException.InnerException as XamlParseException;
            }
            else
            {
                  parseException = new XamlParseException(messageWithLineNumber, lineNumber, linePosition, innerException);
            }

            throw parseException;
        }

        void ThrowExceptionWithLine(string message)
        {
            message += " ";
            message += SR.Get(SRID.ParserLineAndOffset,
                                                 LineNumber.ToString(CultureInfo.CurrentCulture),
                                                 LinePosition.ToString(CultureInfo.CurrentCulture));

            XamlParseException parseException = new XamlParseException(message,
                LineNumber, LinePosition);

            throw parseException;
        }


        #endregion Exceptions

        #region ContextHelpers

        /// <summary>
        /// A delegate to validate the type is valid for the current context.
        /// </summary>
        delegate bool ContentValidator(Type type);

        // Determines Element and Attribute context information.
        // !!Review  - original idea was to have different tags handled different based
        //    on context but things have consolidated into mostly baml tags. Review
        //    if context is still necessary or should simplify

        /// <summary>
        /// Nested class for StackData for each ElementContext that the Tokenizer
        /// encounters.
        /// </summary>
        class ElementContextStackData
        {
            /// <summary>
            /// ContextType
            /// </summary>
            public ElementContextType ContextType
            {
                get { return _contextType; }
                set { _contextType = value; }
            }

            /// <summary>
            /// ContextData
            /// </summary>
            public Object ContextData
            {
                get { return _contextData; }
                set { _contextData = value; }
            }

            /// <summary>
            /// The Type stored in ContextData
            /// </summary>
            public Type ContextDataType
            {
                get
                {
                    DictionaryContextData dcd = _contextData as DictionaryContextData;
                    if (dcd == null)
                    {
                        return _contextData as Type;
                    }
                    else
                    {
                        return dcd.PropertyType;
                    }
                }
            }

            public string NamespaceUri
            {
                get { return _namespaceUri; }
                set { _namespaceUri = value; }
            }

            // When we write a fake start element for a complex property
            // we need to keep track of this so that a fake end element is also written.
            internal bool NeedToWriteEndElement
            {
                get { return _needToWriteEndElement; }
                set { _needToWriteEndElement = value; }
            }

            // When we write a fake start element, we should only check the first child
            // tag for compatibility with the complex property.  This tracks the fact
            // that the first child has been read or not
            internal bool FirstChildRead
            {
                get { return _firstChildRead; }
                set { _firstChildRead = value; }
            }

            // Set when the current element has a content property attribute instead
            // of using IAddChild.
            internal bool IsContentPropertySet
            {
                get
                {
                    // Confirm that Info and Name are in sync (either both set or both null
                    Debug.Assert( (null==ContentPropertyInfo && null==ContentPropertyName)
                               || (null!=ContentPropertyInfo && null!=ContentPropertyName));
                    return (null != ContentPropertyInfo);
                }
            }

            internal PropertyInfo ContentPropertyInfo
            {
                get { return _contentPropertyInfo; }
                set { _contentPropertyInfo = value; }
            }

            internal string ContentPropertyName
            {
                get { return _contentPropertyName; }
                set { _contentPropertyName = value; }
            }

            internal Type ChildPropertyType
            {
                get { return _childPropertyType; }
                set { _childPropertyType = value; }
            }

            /*
            private Type _collectionItemType = null;
            internal Type CollectionItemType
            {
                get { return _collectionItemType; }
                set { _collectionItemType = value; }
            }
            */

            // The name of the child tag under this context item.  This is used for error
            // reporting when we determine at context pop time that the children may
            // be incorrect or in the wrong state.
            internal void SetChildTag(string value)
            {
                _childTag = value;
            }

            // The name of the child tag under this context item.  This is used for error
            // reporting when we determine at context pop time that the children may
            // be incorrect or in the wrong state, and just returns the local name without
            // the namespace information that my be pre-pended
            internal string ChildTagLocalName
            {
                get
                {
                    return _childTag == null ?
                        string.Empty :
                        _childTag.Substring(_childTag.LastIndexOf('.') + 1);
                }
            }

            // The complex properties that are 'children' of this element.  This is used
            // to track duplicate complex properties and is lazy initialized, since not all
            // elements have complex property children.
            internal HybridDictionary ComplexProperties
            {
                get
                {
                    if (_complexProperties == null)
                    {
                        _complexProperties = new HybridDictionary();
                    }
                    return _complexProperties;
                }
            }

            //Can this element take multiple children?
            //We'll only set this when we try to add the 2nd content child to an object.
            //We use IsCollectionChecked to skip the check on the 3rd - Nth.
            internal bool IsContentPropertyACollection
            {
                get
                {
                    if (!_isCollectionChecked)
                    {
                        _isContentPropertyACollection = XamlReaderHelper.IsACollection(this);
                        _isCollectionChecked = true;
                    }
                    return _isContentPropertyACollection;
                }
            }

            //Are we parsing Property elements or Content?
            internal ParsingContent ContentParserState
            {
                get { return (_contentParsingState); }
                set { _contentParsingState = value; }
            }

            // Are we parsing an empty element?
            internal bool IsEmptyElement
            {
                get { return (_isEmptyElement); }
                set { _isEmptyElement = value; }
            }

            internal bool IsWhitespaceSignificantCollectionAttributeKnown
            {
                get { return _isWhitespaceCollectionAttributeKnown; }
            }

            internal bool IsWhitespaceSignificantCollectionAttributePresent
            {
                get { return _isWhitespaceCollectionAttributePresent; }
                set
                {
                    _isWhitespaceCollectionAttributeKnown = true;
                    _isWhitespaceCollectionAttributePresent = value;
                }
            }

            bool _needToWriteEndElement = false;
            bool _firstChildRead = false;
            ParsingContent _contentParsingState = ParsingContent.Before;
            bool _isContentPropertyACollection = false;
            bool _isCollectionChecked = false;
            bool _isEmptyElement = false;
            bool _isWhitespaceCollectionAttributeKnown = false;
            bool _isWhitespaceCollectionAttributePresent = false;
            PropertyInfo _contentPropertyInfo;
            string _contentPropertyName;
            ElementContextType _contextType;
            string _namespaceUri;
            Object _contextData;
            string _childTag;
            Type _childPropertyType;
            HybridDictionary _complexProperties;
        }

        // Nested class to hold information about an unknown start tag.  This information
        // is needed when the matchinig unknown end tag is written out.
        class UnknownData
        {
            public UnknownData(
                string localName,
                string namespaceURI)
            {
                LocalName = localName;
                NamespaceURI = namespaceURI;
            }

            public string LocalName;
            public string NamespaceURI;
        }

        private void SplitPropertyElementName(string longName, out string ownerName, out string propName)
        {
            int idx = longName.LastIndexOf('.');
            if(idx != -1)
            {
                ownerName = longName.Substring(0, idx);
                propName = longName.Substring(idx + 1);
            }
            else
            {
                ownerName = null;
                propName = longName;
            }
        }

        /// <summary>
        /// Determine what attribute context. This can be property, routed event or
        /// clr event.
        /// </summary>
        /// <remarks>
        /// Note that this does not resolve full class-and-type names, but only the
        /// raw type name -- class prefixes must be resolved before calling this function.
        /// </remarks>
        /// <returns>AttributeContext for the Attribute</returns>
        private AttributeContext GetAttributeContext(
                Type elementBaseType,
                string elementBaseTypeNamespaceUri,
                string attributeNamespaceUri,
                string attributeLocalName,
            out Object dynamicObject,         // resolved object.
            out string assemblyName,          // assemblyName the declaringType is found in
            out string declaringTypeFullName, // type FullName of the object that the field is on
            out Type declaringType,         // type of the object that the field is on
            out string dynamicObjectName)     // name of the dynamicObject if found one
        {
            AttributeContext attributeContext = AttributeContext.Unknown;

            dynamicObject = null;
            assemblyName = null;
            declaringTypeFullName = null;
            declaringType = null;
            dynamicObjectName = null;

            // Known definition namespace attributes such as Key and UID should be checked for
            // first so that we avoid reflecting for properties that we know will not be found.
            // Performance problems were caused by too much reflection data being cached
            // if we don't do this check and there is a UID on every element.
            if (attributeNamespaceUri.Equals(DefinitionNamespaceURI))
            {
                switch (attributeLocalName)
                {
                    case DefinitionUid:
                    case DefinitionName:
                    case DefinitionRuntimeName:
                    case DefinitionCodeTag:
                    case DefinitionClass:
                    case DefinitionFieldModifier:
                    case DefinitionSubclass:
                    case DefinitionClassModifier:
                    case DefinitionShared:
                    case DefinitionTypeArgs:
                        return AttributeContext.Code;
                }
            }

            // We have a special check for the Metro xaml namespace, which should
            // only allow Key attributes.  Anything else is an error.  
            if (attributeNamespaceUri.Equals(DefinitionMetroNamespaceURI))
            {
                if (attributeLocalName == DefinitionName)
                {
                    return AttributeContext.Code;
                }
                else
                {
                    ThrowException(SRID.ParserMetroUnknownAttribute,
                                   attributeLocalName,
                                   DefinitionMetroNamespaceURI);
                }
            }

            string ownerName;
            string propName;
            SplitPropertyElementName(attributeLocalName, out ownerName, out propName);

            attributeContext = GetDottedAttributeContext(
                        elementBaseType,
                        elementBaseTypeNamespaceUri,
                        attributeNamespaceUri,
                        ownerName,
                        propName,
                        ref dynamicObject,         // resolved object.
                        ref assemblyName,          // assemblyName the declaringType is found in
                        ref declaringTypeFullName, // type FullName of the object that the field is on
                        ref declaringType,         // type of the object that the field is on
                        ref dynamicObjectName);    // name of the dynamicObject if found one

            return attributeContext;
        }


        private AttributeContext GetDottedAttributeContext(
                                Type elementBaseType,
                                string elementBaseTypeNamespaceUri,
                                string attributeNamespaceUri,
                                string ownerName,
                                string propName,
                            ref Object dynamicObject,         // resolved object.
                            ref string assemblyName,          // assemblyName the declaringType is found in
                            ref string declaringTypeFullName, // type FullName of the object that the field is on
                            ref Type declaringType,         // type of the object that the field is on
                            ref string dynamicObjectName)     // name of the dynamicObject if found one
        {

            AttributeContext attributeContext = AttributeContext.Unknown;

            // The XamlTypeMapper only uses the LineNumber and LinePosition for the
            // sake of displaying debugging information when an exception occurs.
            // Technically, it should not store these values, but rather retrieve them
            // from the XamlReaderHelper.  The TypeMapper was not updating these
            // values until XamlParser called ProcessXamlNode, and even then not in
            // all cases (e.g. Property okay, PropertyComplex not).
            XamlTypeMapper.LineNumber = LineNumber;
            XamlTypeMapper.LinePosition = LinePosition;

            // First, check if this is a CLR property using Static setter name
            // matching or property info lookups on element base type.
            MemberInfo mi = XamlTypeMapper.GetClrInfoForClass(false,
                                                elementBaseType,
                                                attributeNamespaceUri,
                                                propName,
                                                ownerName,
                                            ref dynamicObjectName);

            if (null == mi)
            {
                // Check if this is a CLR event using GetEvent type
                // lookup methods.
                MemberInfo eventMember = (MemberInfo)XamlTypeMapper.GetClrInfoForClass(
                                        true,
                                        elementBaseType,
                                        attributeNamespaceUri,
                                        propName,
                                        ownerName,
                                    ref dynamicObjectName);

                if (null != eventMember)
                {
                    attributeContext = AttributeContext.ClrEvent;
                    dynamicObject = eventMember;
                    declaringType = eventMember.DeclaringType;
                    declaringTypeFullName = declaringType.FullName;
                    assemblyName = declaringType.Assembly.FullName;
                }

                // CLR properties can also occur on MarkupExtension subclasses, and
                // in these cases we may have to tack on "Extension" onto the given
                // type name and look for that class too.
                else if (null != ownerName)
                {
                    string globalClassName = ownerName + "Extension";
                    mi = XamlTypeMapper.GetClrInfoForClass(false,
                                                   elementBaseType,
                                                   attributeNamespaceUri,
                                                   propName,
                                                   globalClassName,
                                               ref dynamicObjectName);
                }

                // Finally, check for the x: namespace.  This has to be after the previous check,
                // or we'll miss Type.TargetName.
                else if (attributeNamespaceUri.Equals(DefinitionNamespaceURI))
                {
                    // check for x: Tag
                    attributeContext = AttributeContext.Code;
                    Debug.Assert(null == dynamicObject,"DynamicObject should be null for code");
                }
            }

            if (null != mi)
            {
                // For attributes that are not qualified using a class name
                // (ie - do NOT have a '.' in the attribute name), check
                // that the namespace of the base type or the declaring type
                // is the same as that of the attribute, or if the attribute is in
                // the default namespace.  Any other prefix is considered an error.
                // Note that the string.Empty check is cheap but superfluous check
                // that is made to screen out the common case where the namespace
                // prefix is empty.
                string namespacePrefix = XmlReader.Prefix;
                if (namespacePrefix != string.Empty)
                {
                    if (null == ownerName &&
                        attributeNamespaceUri != elementBaseTypeNamespaceUri &&
                        attributeNamespaceUri != XmlReader.LookupNamespace(""))
                    {
                        ThrowException(SRID.ParserAttributeNamespaceMisMatch,
                                       propName,
                                       elementBaseTypeNamespaceUri);
                    }
                }

                attributeContext = AttributeContext.Property;
                dynamicObject = mi;
                declaringType = mi.DeclaringType;
                declaringTypeFullName = declaringType.FullName;
                assemblyName = declaringType.Assembly.FullName;
            }

            return attributeContext;
        }

        /// <summary>
        /// Returns the name of the property designated as the content property of the class.
        /// Returns null if no content property is set for the type.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        static string GetContentPropertyName(Type type)
        {
            short id = BamlMapTable.GetKnownTypeIdFromType(type);
            string contentProperty = null;

            if (0 != id)
            {
                KnownElements knownElement = (KnownElements)(-id);
                contentProperty = KnownTypes.GetContentPropertyName(knownElement);
                return contentProperty;
            }

#if !PBTCOMPILER
            AttributeCollection attributes = TypeDescriptor.GetAttributes(type);
            if (attributes != null)
            {
                ContentPropertyAttribute cpa = attributes[typeof(ContentPropertyAttribute)] as ContentPropertyAttribute;
                if (cpa != null)
                {
                    contentProperty = cpa.Name;
                }
            }
#else
            if (KnownTypes.Types[(int)KnownElements.Application].IsAssignableFrom(type) ||
                KnownTypes.Types[(int)KnownElements.ResourceDictionary].IsAssignableFrom(type))
            {
                return null;
            }

            Type baseType = type;
            Type attrType = KnownTypes.Types[(int)KnownElements.ContentPropertyAttribute];
            Type DOType = KnownTypes.Types[(int)KnownElements.DependencyObject];
            Type FEType = KnownTypes.Types[(int)KnownElements.FrameworkElement];
            Type FCEType = KnownTypes.Types[(int)KnownElements.FrameworkContentElement];

            // Keep looking up the base class hierarchy until an appropriate base class is reached.
            while (null != baseType &&
                   DOType != baseType &&
                   FEType != baseType &&
                   FCEType != baseType &&
                   null == contentProperty)
            {
                // CPA with no args is valid and would mean that this type is overriding a base CPA
                contentProperty = ReflectionHelper.GetCustomAttributeData(baseType, attrType, true);
                baseType = baseType.BaseType;
            }
#endif
            if (contentProperty == string.Empty)
            {
                contentProperty = null;
            }

            return contentProperty;
        }



        /// <summary>
        /// Called by ReadXAML when an Element node is encountered
        /// </summary>
        /// <returns>true if parsing should continue</returns>
        bool ReadElementNode()
        {
            // element can be of the following types
            //  - Standard DependencyObjects to put in the Tree
            //  - <x: tags or other tags used by the Compiler
            //  - .net objects

            // Get the IsEmptyElement value before getting attributes because
            // attribute loop will reset the value.
            bool isEmptyElement = XmlReader.IsEmptyElement;
            bool endTagHasBeenRead = false;

            // Put an item on the context stack for this element.  The ContextType
            // may be modified by the call to CompileBamlTag.
            ElementContextStackData elementContextStackData = new ElementContextStackData();
            elementContextStackData.IsEmptyElement = isEmptyElement;

            // If we have a parent stack, this context is the same as the parent
            // by default.
            if (null == CurrentContext)
            {
                elementContextStackData.ContextType = ElementContextType.Default;
            }
            else
            {
                if(ShouldImplyContentProperty())
                {
                    ElementContextStackData CpaStackData = new ElementContextStackData();
                    CpaStackData.ContextType = ElementContextType.Default;
                    ElementContextStack.Push(CpaStackData);
                    ParserContext.PushScope();

                    CompileContentProperty(ParentContext);

                    ElementContextStack.Pop();
                    ParserContext.PopScope();
                }
                elementContextStackData.ContextType = CurrentContext.ContextType;
            }

            ElementContextStack.Push(elementContextStackData);
            ParserContext.PushScope();

            // Compile this tag.
            // Handler should return with  the TextReader position either at the
            // current Node if plan on reading children or at the End of the Current Node.
            CompileBamlTag(XmlNodeType.Element, ref endTagHasBeenRead);

            // if the Element is empty or the caller read past the endTag then
            // call EndElement so Start and ends are balanced.
            if (isEmptyElement)
            {
                //Review - should only read past the EndTag for literal or x:
                // if read pastEnd tag for something that turns into an element
                // the proper EndElementRecord won't be generated so we should either
                // add the logic to make sure this doesn't happen or not guarantee
                // the endElement.
                if (!endTagHasBeenRead)
                {
                    CompileBamlTag(XmlNodeType.EndElement, ref endTagHasBeenRead);
                    Debug.Assert(false == endTagHasBeenRead, "Read past end tag on end tag");
                }

                // Empty Complex Properties of type Dictionary should be popped now as
                // a start and end element tag will be added making it really non empty.
                // Ideally the parser should recognize this case and not add any implicit
                // start\end elements in this case. If\when that is fixed, this code below
                // can be removed.
                if (CurrentContext != null && CurrentContext.ContextType == ElementContextType.PropertyIDictionary)
                {
                    ElementContextStack.Pop();
                    ParserContext.PopScope();
                }
            }
            else if (endTagHasBeenRead)
            {
                // Note that we should not pop the stack here since that context info
                // might be needed when the empty element's attributes are being processed.
                ElementContextStack.Pop();
                ParserContext.PopScope();
            }

            return true;

        }

        /// <summary>
        /// Helper function called by ReadXAML when an EndElementNode is encountered.
        /// If endTagHasBeenRead is true, then we have already read past the end tag, so
        /// we don't need to advance the reader any further.
        /// </summary>
        /// <returns>returns true if parse should continue</returns>
        internal bool ReadEndElementNode(bool endTagHasBeenRead)
        {

            // call appropriate handler
            // handler should return still at the current position.

            CompileBamlTag(XmlNodeType.EndElement,
                           ref endTagHasBeenRead);

            // pop the stack
            ElementContextStack.Pop();
            ParserContext.PopScope();

            return true;
        }


        /// <summary>
        /// Helper function called by ReadXAML as the default for NodeTypes.
        /// </summary>
        /// <returns>false if parse should be stopped</returns>
        bool ReadGenericXmlNode()
        {
            // for any other nodes just call the appropriate context.
            // make sure stack has anything because may encounter nodes before hit first Element
            // tag <?xml, whitespace, etc. Add asserts this only happens at the proper depth

            // go ahead and call the designer stuff here. should really do this
            // in each context but since they all handle it the same do
            // it here for now.

            bool endTagHasBeenRead = false;

            // if normal then do normal, if skip then just continue.

            if (null != CurrentContext)
            {

                CompileBamlTag(XmlReader.NodeType, ref endTagHasBeenRead);
                Debug.Assert(false == endTagHasBeenRead);

                // If we're in the context of an IDictionary or complex property, we
                // have to keep reading until we get to an element in case there is an
                // element tag to replace the synthesized tag we created earlier.
                if (CurrentContext.ContextType == ElementContextType.PropertyIDictionary ||
                    CurrentContext.ContextType == ElementContextType.PropertyComplex)
                {
                    _readAnotherToken = true;
                }
            }
            else
            {
                // if no context see if its a valid top-evel node such as Text or CDATA
                // and if so handle.
                switch (XmlReader.NodeType)
                {
                    case XmlNodeType.Text:
                    case XmlNodeType.CDATA:
                    case XmlNodeType.SignificantWhitespace:
                    case XmlNodeType.Whitespace:
                        CompileText(XmlReader.NodeType, XmlReader.Value);
                        XmlReader.Read(); // move to the next node.
                        break;

                    case XmlNodeType.ProcessingInstruction:
                        CompilePI();
                        XmlReader.Read(); // move to the next node.
                        break;

                    // Custom entity references are not currently supported.
                    case XmlNodeType.EntityReference:
                        ThrowException(SRID.ParserEntityReference, XmlReader.Name);
                        break;

                    default:
                        XmlReader.Read();
                        break;
                }

            }

            return true;
        }


        #endregion // ContextHelpers

        #region DefinitionContext

        // Handle tokenization of x: tags.  Note that x:Array is not handled here, but
        // is handled in CompileBamlTag since it is treated as a special modifier to
        // a complex property tag and is not really a standalone element in its own right.
        private void CompileDefTag(
            XmlNodeType xmlNodeType,
            ref bool endTagHasBeenRead)
        {

            bool isEmpty = XmlReader.IsEmptyElement;

            switch (xmlNodeType)
            {
                case XmlNodeType.Element:
                    {
                        WriteDefTag(XmlReader.Name);

                        // !!!!temporary until stop calling WriteDef with the Reader
                        // we set this to true so the Caller treats logic as
                        // if we read past the end tag which will actually be
                        // done when the def tag is processed
                        endTagHasBeenRead = true;
                        break;
                    }

                case XmlNodeType.EndElement:
                    break;

                case XmlNodeType.Text:
                case XmlNodeType.CDATA:
                case XmlNodeType.SignificantWhitespace:
                case XmlNodeType.Whitespace:
                    CompileText(XmlReader.NodeType, XmlReader.Value);
                    break;

                default:
                    break;
            }

            // if we haven't alread readPast the end tag and the element is
            // an empty element then read
            // if empty get on the EndElement Call
            if (!isEmpty && !endTagHasBeenRead)
            {
                // if we haven't read past the end for some reason go ahead and move now
                XmlReader.Read();
            }
        }

        #endregion DefinitionContext

        #region BamlContext

        // main context for tags use to have a separate one for ClrObject but now everything
        // is combined.

        #region Attributes


        // Processes attributes that are scoped to the entire element, regardless
        // of what order they are specified in.
        // Collect all namespace directives, and check for xml:space and xml:lang
        // special properties.  If xml:lang is found, return the PropertyInfo that
        // was set on the current element for this property.
        //this method modifies the position of the XmlReader by iterating over the
        //attributes, it is the responsibility of the caller to move back to the first
        //attribute after calling this method.
        private PropertyInfo HandleElementScopedAttributes(
            Type elementType,
            string namespaceURI,
            out bool attributeFound
            )
        {
            string xmlLang = null;
            string xmlSpace = null;
            string freezeValue = null;
            PropertyInfo xmlLangProperty = null;

            attributeFound = false;

            // first loop through adding any namespaces
            // while looping also see if found any space or lang attributes
            bool moreAttributes = XmlReader.MoveToFirstAttribute();
            while (moreAttributes)
            {
                const string NamespacePrefix = "xmlns:";

                bool namespaceAttribute = false;
                string attribName = XmlReader.Name;
                string attribLocalName = XmlReader.LocalName;
                string attributeNamespaceUri = XmlReader.LookupNamespace(XmlReader.Prefix);
                string attribValue = XmlReader.Value;
                string prefix = string.Empty;

                // Look for xmlns: ...
                if (attribName.StartsWith(NamespacePrefix, StringComparison.Ordinal))
                {
                    prefix = attribName.Substring(NamespacePrefix.Length);
                    namespaceAttribute = true;

                    // HandleElementScopedAttributes must be kept in-sync with IsElementScopedAttribute
                    Debug.Assert(IsElementScopedAttribute(attribName, attribLocalName, attributeNamespaceUri));
                    attributeFound = true;
                }
                // Look for xmlns
                else if (attribName.Equals(XmlnsDeclaration))
                {
                    namespaceAttribute = true;

                    // HandleElementScopedAttributes must be kept in-sync with IsElementScopedAttribute
                    Debug.Assert(IsElementScopedAttribute(attribName, attribLocalName, attributeNamespaceUri));
                    attributeFound = true;
                }
                // Look for xml:space
                else if (attribName.Equals(XmlAttributeProperties.XmlSpaceString))
                {
                    xmlSpace = attribValue;

                    // HandleElementScopedAttributes must be kept in-sync with IsElementScopedAttribute
                    Debug.Assert(IsElementScopedAttribute(attribName, attribLocalName, attributeNamespaceUri));
                    attributeFound = true;
                }
                // Look for xml:lang
                else if (attribName.Equals(XmlAttributeProperties.XmlLangString))
                {
                    xmlLang = attribValue;

                    // HandleElementScopedAttributes must be kept in-sync with IsElementScopedAttribute
                    Debug.Assert(IsElementScopedAttribute(attribName, attribLocalName, attributeNamespaceUri));
                    attributeFound = true;
                }
                // Look for PresentationOptions:Freeze
                else if (IsAttributePresentationOptionsFreeze(attribLocalName, attributeNamespaceUri))
                {
                    freezeValue = attribValue;

                    // HandleElementScopedAttributes must be kept in-sync with IsElementScopedAttribute
                    Debug.Assert(IsElementScopedAttribute(attribName, attribLocalName, attributeNamespaceUri));
                    attributeFound = true;
                }

                // HandleElementScopedAttributes must be kept in-sync with IsElementScopedAttribute to avoid processing
                // the attribute again during WriteAttribute.
                //
                // If no element-scoped attribute has been found (including this one), then IsElementScopedAttribute must return
                // false for this attribute
                Debug.Assert (attributeFound || !IsElementScopedAttribute(attribName, attribLocalName, attributeNamespaceUri));

                if (namespaceAttribute)
                {
                    ParserContext.XmlnsDictionary[prefix] = attribValue;
                    WriteNamespacePrefix(prefix, attribValue);
                }

                moreAttributes = XmlReader.MoveToNextAttribute();
            }

            // if found space or lang tags write them out.
            object dpOrMi = null;
            if (null != xmlSpace)
            {
                ParserContext.XmlSpace = xmlSpace;

                // Set the XmlSpace DependencyProperty if the current object
                // is a DependencyObject.  Otherwise, ignore it.

                Type dependencyObjectType = KnownTypes.Types[(int)KnownElements.DependencyObject];
                if (CurrentContext.ContextType == ElementContextType.Default &&
                    dependencyObjectType.IsAssignableFrom(CurrentContext.ContextDataType))
                {
                    // Do not set via Dependency Property, because we need to
                    //  use a CLR setter instead.
                    // Use XmlAttributeProperties::XmlSpaceSetter to get a MethodInfo
                    dpOrMi = XmlAttributeProperties.XmlSpaceSetter;

                    WriteProperty("XmlSpace", dpOrMi,
                        XamlTypeMapper.PresentationFrameworkDllName, XmlAttributesFullName, xmlSpace,
                        BamlAttributeUsage.XmlSpace);
                }
            }

            if (null != xmlLang && elementType != null)
            {
                ParserContext.XmlLang = xmlLang;

                // Find the property on the current type that will hold the xml:lang
                // attribute value at runtime, and create a property set for this
                // property.
                xmlLangProperty =
                    ParserContext.XamlTypeMapper.GetXmlLangProperty(namespaceURI, elementType.Name);

                // If there is no property, then just store the xml:lang value in the
                // ParserContext and leave xmlLangProperty as null.
                if (xmlLangProperty != null)
                {
                    string assemblyName = elementType.Assembly.FullName;

                    WriteProperty(xmlLangProperty.Name, xmlLangProperty,
                        assemblyName, elementType.FullName, xmlLang,
                        BamlAttributeUsage.XmlLang);
                }
            }

            // Write Freeze after the Xml namespaces & attributes have been resolved.

            if (freezeValue != null)
            {
                WritePresentationOptionsAttribute(PresentationOptionsFreeze, freezeValue);
            }

            return xmlLangProperty;
        }

        // Scans for mapping
        private void ScanForMappingProtocols()
        {
            bool moreAttributes = XmlReader.MoveToFirstAttribute();
            while (moreAttributes)
            {
                string attribName = XmlReader.Name;

                if (attribName.Equals(XmlnsDeclaration, StringComparison.Ordinal) ||
                    attribName.StartsWith("xmlns:", StringComparison.Ordinal))
                {
                    string attribValue = XmlReader.Value;

                    if (String.IsNullOrEmpty(attribValue))
                    {
                        ThrowException(SRID.ParserUndeclaredNS, String.Empty);
                    }

                    if (attribValue.StartsWith(MappingProtocol, StringComparison.Ordinal))
                        HandleMappingProtocol(attribValue);
                }

                moreAttributes = XmlReader.MoveToNextAttribute();
            }
            XmlReader.MoveToElement();
        }

        // Parses the mapping protocol uri that specifies the namespace and assembly
        // referenced by the uri.
        private void HandleMappingProtocol(
                string mappingUri)
        {
            MappingParser parser = new MappingParser(mappingUri, MappingProtocol.Length);
            if (!parser.Parse())
                ThrowException(SRID.ParserMappingUriInvalid, mappingUri);

            // Always set up the mapping for this in the XamlTypeMapper immediately upon seeing the
            // mapping URI, since it can be needed in the very next read operation and must be in place.
            if (!XamlTypeMapper.PITable.Contains(mappingUri))
            {
                string assemblyName = parser.Assembly;
                bool isLocalAssembly = assemblyName == null || assemblyName.Length < 1;
#if PBTCOMPILER
                if (isLocalAssembly)
                    assemblyName = ReflectionHelper.LocalAssemblyName;
#endif
                ClrNamespaceAssemblyPair usingData = new ClrNamespaceAssemblyPair(parser.Namespace, assemblyName);

#if PBTCOMPILER
                usingData.LocalAssembly = isLocalAssembly;
#endif

                XamlTypeMapper.PITable.Add(mappingUri, usingData);
            }

            // Generate a pseudo mapping instruction in the BAML from the URI
            WritePI(mappingUri, parser.Namespace, parser.Assembly);
        }

        #region Mapping Protocol helper classes

        /// <summary>
        ///     This is a scanner that tokenizes a mapping Uri.
        ///     A mapping Uri is the xmlns attribute value shown below.
        ///         <Page xmlns:sys="clr-namespace:System;assembly=mscorlib">
        ///         ...
        ///         </Page>
        /// </summary>
        private class MappingScanner
        {
            private string _text;
            private int _start;
            private int _current;

            /// <summary>
            ///     Constructor for the MappingScanner.
            ///     The offset is usually the position of the
            ///     ':' that follows "clr-namespace". An xmlns
            ///     attribute value that starts with "clr-namespace"
            ///     is interpreted as a mapping Uri.
            /// </summary>
            public MappingScanner(string text, int offset)
            {
                _text = text + '\x0';
                _current = offset;
            }

            /// <summary>
            ///     This method identifies the next token to follow
            ///     the _start location within the _text string. It also
            ///     advances the _start location to the end of the
            ///     identified token.
            ///     Ident (meaning Identifier), Number, special
            ///     character (such as a ; delimiter), Error and End
            ///     are examples of tokens returned from this method.
            /// </summary>
            public char NextToken()
            {
                int current = _current;
                _start = current;
                char c = _text[current++];
                if ((int)c >= 256)
                    c = Letter;
                switch (CharCodes[(int)c])
                {
                    case '\\':
                        if (_text[current++] == '\0')
                            goto case Error;
                        goto case Letter;

                    case Letter:
                        while (true)
                        {
                            c = _text[current++];
                            if ((int)c >= 256) continue;
                            switch (CharCodes[(int)c])
                            {
                                case Letter:
                                case Number:
                                    continue;
                                case '\\':
                                    if (_text[current++] == '\0')
                                    {
                                        _current = current - 1;
                                        return Error;
                                    }
                                    continue;
                                default:
                                    break;
                            }
                            break;
                        }
                        _current = current - 1;
                        return Ident;
                    default:
                        _current = current;
                        return c;
                    case Number:
                        do
                        {
                            c = _text[current++];
                        }
                        while ((int)c < 256 && CharCodes[(int)c] == Number);
                        _current = current - 1;
                        return Number;
                    case Error:
                        _current = current;
                        return Error;
                    case End:
                        return End;
                }
            }

            /// <summary>
            ///     This method is typically called after having found a token by calling
            ///     NextToken(). This method tells you the real value of the token. For
            ///     example if the token is an identifier then this method returns the
            ///     string representing the identifier.
            /// </summary>
            public string TokenValue()
            {
                return _text.Substring(_start, _current - _start);
            }

            /// <summary>
            ///     This is an equality check between the current token
            ///     value that the scanner is positioned at and the given string.
            /// </summary>
            public bool TokenEquals(string value)
            {
                int len = _current - _start;
                return len == value.Length && String.CompareOrdinal(value, 0, _text, _start, len) == 0;
            }

            public int Start { get { return _start; } }

            public const char End = '\x0';
            public const char Ident = '\x1';
            public const char Error = '\x2';

            private const char Number = '1';
            private const char Letter = 'a';

            static readonly char[] CharCodes;

            static MappingScanner()
            {
                CharCodes = new char[256];
                for (char i = '\x1'; (int)i < 256; i++)
                {
                    switch (i)
                    {
                        case '.':
                        case ':':
                        case ';':
                        case '-':
                        case '=':
                        case ',':
                        case '\\':
                        case '+':
                            CharCodes[(int)i] = i;
                            break;
                        case '_':
                            CharCodes[(int)i] = Letter;
                            break;
                        default:
                            if (Char.IsLetter(i))
                                CharCodes[(int)i] = Letter;
                            else if (Char.IsDigit(i))
                                CharCodes[(int)i] = Number;
                            else
                                CharCodes[(int)i] = Error;
                            break;
                    }
                }
            }
        }

        /// <summary>
        ///     This is a parser that parses a mapping Uri.
        ///     A mapping Uri is the xmlns attribute value shown below.
        ///         <Page xmlns:sys="clr-namespace:System;assembly=mscorlib">
        ///         ...
        ///         </Page>
        ///     It employs a MappingScanner to tokenize the mapping Uri string.
        ///     Then it performs syntactic checks on the token values and finally
        ///     produces the strings representing the clr namespace and the
        ///     assembly name to be referenced.
        /// </summary>
        private class MappingParser
        {
            MappingScanner _scanner;
            string _namespace;
            string _assembly;
            char _token;

            public MappingParser(string mapping, int offset)
            {
                _assembly = "";
                _scanner = new MappingScanner(mapping, offset);
            }

            /// <summary>
            ///     This is the main entry-point into the parser. This is the routine that
            ///     repeatedly invokes the scanner and then strings the token values
            ///     together to ultimately produce the clr namespace and assembly
            ///     name strings.
            /// </summary>
            public bool Parse()
            {
                try
                {
                    // Fetch the next token
                    Next();

                    // Parse until we've found the clr namespace string
                    _namespace = ParseNamespace();

                    if (_token == ';')
                    {
                        Next();

                        // If we found a ';' delimiter then it must be followed
                        // by the keyword "assembly" and then another delimiter '='.
                        Expect("assembly");
                        Expect('=');

                        // Whatever follows constitutes the assembly name
                        _assembly = ParseAssembly();
                    }
                    Expect(MappingScanner.End);
                }
                catch (MappingParseError)
                {
                    // Mapping error has a lot of information its throwing away about
                    // why this uri is bad. There currently is no good way to map the offset
                    // of the string to a column number and the error information is too
                    // confusing without the correct column number.
                    return false;
                }

                return true;
            }

            /// <summary>Returns the clr namespace string</summary>
            public string Namespace { get { return _namespace; } }

            /// <summary>Returns the assembly name string</summary>
            public string Assembly { get { return _assembly; } }

            /// <summary>
            ///     Clr namespace is a series of identifiers separated by '.'
            ///     delimiters.
            /// </summary>
            private string ParseNamespace()
            {
                StringBuilder name = new StringBuilder();
                while (_token == MappingScanner.Ident)
                {
                    name.Append(_scanner.TokenValue());
                    if (Next() == '.')
                    {
                        name.Append('.');
                        Next();
                    }
                }
                return name.ToString();
            }

            /// <summary>
            ///     The assembly name constitutes the part of the attribute
            ///     value that lies beyond the "assembly=" keyword.
            /// </summary>
            private string ParseAssembly()
            {
                StringBuilder assembly = new StringBuilder();
                while (_token != MappingScanner.End && _token != MappingScanner.Error)
                {
                    assembly.Append(_scanner.TokenValue());
                    Next();
                }
                return assembly.ToString();
            }

            private char Next()
            {
                // Fetch the next token
                _token = _scanner.NextToken();
                return _token;
            }

            /// <summary>
            ///     The next token value that is found must match
            ///     the given string else we will raise a syntax error.
            /// </summary>
            private void Expect(string ident)
            {
                if (_token != MappingScanner.Ident || !_scanner.TokenEquals(ident))
                    Error(ident);
                Next();
            }

            /// <summary>
            ///     The next token value that is found must match
            ///     the given character else we will raise a syntax error.
            /// </summary>
            private void Expect(char token)
            {
                if (_token != token)
                    Error(token);
                Next();
            }

            /// <summary>
            ///     This is the exception that is thrown when we encounter
            ///     a syntax error while parsing the mapping Uri.
            /// </summary>
            private class MappingParseError : Exception
            {
                public readonly int Offset;
                public readonly char Expected;
                public readonly char Received;
                public readonly string Ident;

                public MappingParseError(int offset, char expected, char received)
                {
                    Offset = offset;
                    Expected = expected;
                    Received = received;
                }

                public MappingParseError(int offset, char expected, char received, string ident)
                    : this(offset, expected, received)
                {
                    Ident = ident;
                }
            }

            private void Error(char token)
            {
                throw new MappingParseError(_scanner.Start, token, _token);
            }

            private void Error(string ident)
            {
                throw new MappingParseError(_scanner.Start, MappingScanner.Ident, _token, ident);
            }
        }
        #endregion

        private string ResolveAttributeNamespaceURI(string prefix, string name, string parentURI)
        {
            string attribNamespaceURI;
            if(!String.IsNullOrEmpty(prefix))
            {
                attribNamespaceURI = XmlReader.LookupNamespace(prefix);
            }
            else
            {
                // if the prefix was "" then
                // 1) normal properties resolve to the parent Tag namespace.
                // 2) Attached properties resolve to the "" default namespace.
                int dotIndex = name.IndexOf('.');
                if (-1 == dotIndex)
                    attribNamespaceURI = parentURI;
                else
                    attribNamespaceURI = XmlReader.LookupNamespace("");
            }
            if (String.IsNullOrEmpty(attribNamespaceURI))
            {
               ThrowException(SRID.ParserPrefixNSProperty, prefix, name);
            }
            return attribNamespaceURI;
        }

        // Loop through the attributes returned by the XmlReader, writing out
        // appropriate XamlNodes
        private void WriteAttributes(
                Type parentType,             // Type of parent, if known
                string parentTypeNamespace,    // Xml namespace URI where parent is defined
                string unknownTagName,         // non-null if properties belong to unknown tag
                int depth)                  // Reader depth in xml tree
        {
            Debug.Assert(unknownTagName != null || null != parentType);

            // Bool used to avoid searching for element-scoped attributes when
            // we've HandleElementScopedAttributes has already determined they
            // don't exist.
            bool elementScopedAttributeFound;

            // Some special attributes have to be detected before any others because they
            // are scoped to the entire element regardless of what order they are specified
            // in.  Examples include namespaces, xml:lang, and Freeze.  Look for those first.
            HandleElementScopedAttributes(
                parentType,
                parentTypeNamespace,
                out elementScopedAttributeFound
                );

            bool moreAttributes = XmlReader.MoveToFirstAttribute();
            ArrayList markupExtensionList = null;
            ArrayList complexDefAttributesList = null;

            if (moreAttributes)
            {
                // Collect all PropertyInfos, DependencyProperties and MethodInfos
                // that are resolved from the attribute strings so that we can
                // flag duplicates.
                HybridDictionary resolvedProperties = CurrentProperties;

                // Begin insertion mode for attributes of current element. This is so that
                // the nodes collection can know where to add Name first & then Events and
                // then Properties in the order set in the markup.
                TokenReaderNodeCollection.IsMarkedForInsertion = true;

                // now loop through the non xmlns Attributes.
                while (moreAttributes)
                {
                    string attribLocalName = XmlReader.LocalName;
                    string attribName = XmlReader.Name;
                    string attribPrefix = XmlReader.Prefix;
                    string attribValue = XmlReader.Value;
                    int lineNumber = LineNumber;
                    int linePosition = LinePosition;

                    string attribNamespaceURI = ResolveAttributeNamespaceURI(attribPrefix, attribName, parentTypeNamespace);

                    // Element-scoped attributes have already been handled, so only continue
                    // if no element-scoped attributes exist, or if they do exist and this isn't
                    // one of them.
                    if (!elementScopedAttributeFound ||
                        !IsElementScopedAttribute(attribName, attribLocalName, attribNamespaceURI)
                        )
                    {
                        // attribute may map to the following
                        // - class or other code attribute which we should ignore
                        // - DependencyProperty
                        // - RoutedEvent

                        Object dynamicObject = null;
                        string assemblyName = null;
                        string dynamicObjectName = null;
                        string declaringTypeFullName = null;
                        Type declaringType = null;

                        // Determine the type to lookup and if valid see if it
                        // can resolve to an Attribute Context.  Look for
                        // normal clr properties first and if this doesn't work,
                        // try to resolve this as an attached DependencyProperty.
                        AttributeContext attributeContext = AttributeContext.Unknown;
                        if (unknownTagName == null)
                        {
                            // Attempt to get the attribute context only if the
                            //  tag is known.  If the tag is an "Unknown Tag"
                            //  treat all attributes as unknown.
                            attributeContext = GetAttributeContext(parentType,
                                                               parentTypeNamespace,
                                                               attribNamespaceURI,
                                                               attribLocalName,
                                                           out dynamicObject,
                                                           out assemblyName,
                                                           out declaringTypeFullName,
                                                           out declaringType,
                                                           out dynamicObjectName);
                        }
                        else
                        {
                            // Exception: If a "Definition" namespace attribute
                            //  is within an unknown tag, it's not REALLY unknown.
                            if (attribNamespaceURI.Equals(DefinitionNamespaceURI))
                            {
                                attributeContext = AttributeContext.Code;
                            }
                        }

                        // If the property is an Event but the value is a Markup extension
                        // then process it "normally".
                        if (attributeContext == AttributeContext.ClrEvent)
                        {
                            if (MarkupExtensionParser.LooksLikeAMarkupExtension(attribValue))
                            {
                                attributeContext = AttributeContext.Property;
                            }
                        }

                        // handle according to the context
                        switch (attributeContext)
                        {
                            case AttributeContext.Code:
                                WriteDefAttributes(
                                             depth,
                                             attribValue,
                                             attribLocalName,
                                             parentType,
                                         ref complexDefAttributesList,
                                             resolvedProperties,
                                             parentTypeNamespace);
                                break;

                            case AttributeContext.Property:
                                WritePropertyAttribute(
                                            parentType,
                                            resolvedProperties,
                                            attribLocalName,
                                            attribName,
                                            attribNamespaceURI,
                                            attribValue,
                                            dynamicObject,
                                            assemblyName,
                                            declaringTypeFullName,
                                            dynamicObjectName,
                                            declaringType,
                                            lineNumber,
                                            linePosition,
                                            depth,
                                        ref markupExtensionList);
                                break;
#if !PBTCOMPILER
                            case AttributeContext.RoutedEvent:
                                Debug.Assert(null != assemblyName, "property without an AssemblyName");
                                Debug.Assert(null != declaringTypeFullName, "property without a type name");
                                Debug.Assert(null != dynamicObjectName, "property without a field Name");

                                RoutedEvent routedEvent = (RoutedEvent)dynamicObject;
                                WriteRoutedEvent(routedEvent, assemblyName,
                                    declaringTypeFullName, dynamicObjectName, attribValue);

                                break;
#endif

                            case AttributeContext.ClrEvent:
                                // dynamicObject is either the eventInfo for the event or the methodInfo for the Add{EventName}Handler method
                                MemberInfo eventMember = (MemberInfo)dynamicObject;
                                WriteClrEvent(dynamicObjectName, eventMember, attribValue);
                                break;

                            case AttributeContext.Unknown:
                                WriteUnknownAttribute(attribNamespaceURI, attribLocalName,
                                                      attribValue, depth, parentTypeNamespace,
                                                      unknownTagName == null ? parentType.Name : unknownTagName,
                                                      dynamicObject, resolvedProperties);
                                break;

                            default:
                                ThrowExceptionWithLine(
                                    SR.Get(SRID.ParserUnknownAttribute,
                                        attribLocalName,
                                        attribNamespaceURI));
                                break;
                        }
                    }

                    moreAttributes = XmlReader.MoveToNextAttribute();
                }

                // End insertion mode for attributes, as we are done with any Name or Event
                // attributes for current element.
                TokenReaderNodeCollection.IsMarkedForInsertion = false;

                // Process any complex x: attributes.  These occur when something
                // like x:Key contains complex markup that must be expanded.
                if (complexDefAttributesList != null)
                {
                    ArrayList xamlNodes = _extensionParser.CompileDictionaryKeys(complexDefAttributesList, XmlReader.Depth);
                    for (int i = 0; i < xamlNodes.Count; i++)
                    {
                        TokenReaderNodeCollection.Add((XamlNode)xamlNodes[i]);
                    }
                }

                // Process any MarkupExtension attributes that have been found while parsing properties.
                // These must always come after the regular properties to maintain correct ordering
                // of properties and things that behave sort of like children.
                if (markupExtensionList != null)
                {
                    ArrayList xamlNodes = _extensionParser.CompileAttributes(markupExtensionList, XmlReader.Depth);
                    for (int i = 0; i < xamlNodes.Count; i++)
                    {
                        TokenReaderNodeCollection.Add((XamlNode)xamlNodes[i]);
                    }
                }
            }

            if (_definitionScopeType == null &&
                parentType != null &&
                ParentContext != null &&
                KnownTypes.Types[(int)KnownElements.IComponentConnector].IsAssignableFrom(parentType))
            {
                _definitionScopeType = parentType;
            }

            WriteEndAttributes(depth, false);
        }

        // Create the XamlNodes for a Definition namespace attribute
        private void WriteDefAttributes(
            int depth,
            string attribValue,
            string attribLocalName,
            Type parentType,
        ref ArrayList complexDefAttributeList,
            HybridDictionary resolvedProperties,
            string parentTypeNamespace)
        {
            string runtimePropertyName = GetRuntimeNamePropertyName(parentType);
            string parentName = parentType != null ? parentType.Name : string.Empty;

            // review - should only check on the first tag.
            // review - should we enforce all x: grammar here instead
            // of letting it fall through
            if (attribLocalName == DefinitionSynchronousMode)
            {
                if (0 != depth)
                {
                    ThrowException(SRID.ParserSyncOnRoot);
                }

                WriteDefAttribute(attribLocalName, attribValue);

            }
            else if (attribLocalName == DefinitionAsyncRecords)
            {
                if (0 != depth)
                {
                    ThrowException(SRID.ParserAsyncOnRoot);
                }

                WriteDefAttribute(attribLocalName, attribValue);
            }
            else if (IsNameProperty(attribValue, parentName, attribLocalName, DefinitionNamespaceURI, runtimePropertyName))
            {
                // Do not allow ID on abstract classes, classes without default constructors
                // or value types.
                if (parentType != null &&
                    (parentType.IsAbstract ||
                     parentType.GetConstructor(Type.EmptyTypes) == null ||
                     parentType.IsValueType))
                {
                    ThrowException(SRID.ParserNoNameOnType, parentType.Name);
                }

                //Write the matching property record if needed
                bool propertyWritten = WriteNameProperty(attribLocalName, parentType, attribValue, parentTypeNamespace, resolvedProperties, runtimePropertyName);

                if (!propertyWritten)
                {
                    WriteDefAttribute(attribLocalName, attribValue, BamlAttributeUsage.RuntimeName);
                }
            }
            else
            {
                // If the value of x:Key is a MarkupExtension, then
                // the def attribute is represented in something akin to complex
                // property syntax using a special internal DP.
                // This allows the nesting of markup extensions.
                DefAttributeData attribData = null;
                if (attribLocalName == DefinitionName)
                {
                    attribData = _extensionParser.IsMarkupExtensionDefAttribute(parentType,
                                                    ref attribValue, LineNumber, LinePosition, depth);
                }

                if (attribData == null)
                {
                    // Other attributes are just written out as a generic
                    // def attribute.  It is up to the XamlParser or the
                    // BamlRecordReader to determine if it is value or not.
                    WriteDefAttribute(attribLocalName, attribValue);
                }
                else
                {
                    if (attribData.IsSimple)
                    {
                        // If the MarkupExtension does not expand out into a complex property
                        // subtree, but can be handled inline with other properties, then
                        // process it immediately in place of the normal property
                        int colonIndex = attribData.Args.IndexOf(':');
                        string prefix = string.Empty;
                        string typeName = attribData.Args;
                        // Copy typename after the colon (if any)
                        if (colonIndex > 0)
                        {
                            prefix = attribData.Args.Substring(0, colonIndex);
                            typeName = attribData.Args.Substring(colonIndex + 1);
                        }

                        string valueNamespaceURI = XmlReader.LookupNamespace(prefix);
                        string valueAssemblyName = string.Empty;
                        string valueTypeFullName = string.Empty;
                        Type valueElementType = null;
                        Type valueSerializerType = null;

                        bool resolvedTag = GetElementType(false, typeName, valueNamespaceURI, ref valueAssemblyName,
                            ref valueTypeFullName, ref valueElementType, ref valueSerializerType);

                        // If we can't resolve a simple TypeExtension value at compile time,
                        // then write it out as a normal type extension and wait until runtime
                        // since the type may not be visible now.
                        if (resolvedTag)
                        {
                            WriteDefKeyWithType(valueTypeFullName, valueAssemblyName,
                                                valueElementType);
                        }
                        else
                        {
                            // If this is not a "Local" type, then complain, since local types
                            // cannot be resolved now.
                            if (XamlTypeMapper.IsLocalAssembly(valueNamespaceURI))
                            {
                                attribData.IsSimple = false;
                                attribData.Args += "}";
                            }
                            else
                            {
                                ThrowException(SRID.ParserNoType, typeName);
                            }
                        }
                    }
                    if (!attribData.IsSimple)
                    {
                        if (complexDefAttributeList == null)
                        {
                            complexDefAttributeList = new ArrayList(1);
                        }
                        complexDefAttributeList.Add(attribData);
                    }
                    return;
                }
            }

            // The following check should really be made in the XamlParser,
            // but I haven't moved all the validation code that exists in the
            // tokenizer to there yet, so I have to do the check here for
            // duplicate x:Key attributes in a dictionary.
            if (attribLocalName == DefinitionName)
            {
                if (ParentContext == null)
                {
                    ThrowException(SRID.ParserNoDictionaryName);
                }
                ElementContextType pct = ParentContext.ContextType;
                Type pType = ParentContext.ContextData as Type;
                if (ElementContextType.PropertyIDictionary != pct
#if PBTCOMPILER
                    && (ElementContextType.Unknown != pct)  // Compile 1st pass
#endif
#if PBTCOMPILER
                    && !ReflectionHelper.GetMscorlibType(typeof(IDictionary)).IsAssignableFrom(pType))
#else
                    && !typeof(IDictionary).IsAssignableFrom(pType))
#endif
                {
                    ThrowException(SRID.ParserNoDictionaryName);
                }

                DictionaryContextData dictionaryData = ParentContext.ContextData as DictionaryContextData;
                if (dictionaryData != null)
                {
                    object key;
                    // Note that not all keys can be resolved at compile time.  For those that fail,
                    // just use the string value.
                    try
                    {
                        key = XamlTypeMapper.GetDictionaryKey(attribValue, ParserContext);
                    }
                    catch (Exception e)
                    {
                        if (CriticalExceptions.IsCriticalException(e))
                        {
                            throw;
                        }
                        else
                        {
                            key = attribValue;
                        }
                    }

                    if (dictionaryData.ContainsKey(key))
                    {
                        ThrowException(SRID.ParserDupDictionaryKey, attribValue);
                    }
                    else
                    {
                        dictionaryData.AddKey(key);
                    }
                }
            }
        }

        // Create the xamlnodes for a property attribute.
        private void WritePropertyAttribute(
                Type parentType,
                HybridDictionary resolvedProperties,
                string attribLocalName,
                string attribName,
                string attribNamespaceURI,
                string attribValue,
                Object dynamicObject,
                string assemblyName,
                string declaringTypeFullName,
                string dynamicObjectName,
                Type declaringType,
                int lineNumber,
                int linePosition,
                int depth,
            ref ArrayList markupExtensionList)
        {
            Debug.Assert(null != assemblyName, "property without an AssemblyName");
            Debug.Assert(null != declaringTypeFullName, "property without a type name");
            Debug.Assert(null != dynamicObjectName, "property without a field Name");

            CheckDuplicateProperty(resolvedProperties, attribLocalName, dynamicObject);
            // Determine if the property is read only or if it was
            // already assigned using xml:lang and complain if so.
            PropertyInfo propInfo = dynamicObject as PropertyInfo;
            if (ControllingXamlParser != null)
            {
                bool propertyCanWrite;
                if (propInfo != null)
                {
                    propertyCanWrite = propInfo.CanWrite;
                }
#if !PBTCOMPILER
                else if (dynamicObject is DependencyProperty &&
                    (parentType != null && KnownTypes.Types[(int)KnownElements.DependencyObject].IsAssignableFrom(parentType)))
                {
                    propertyCanWrite = !((DependencyProperty)dynamicObject).ReadOnly;
                }
#endif
                else if (dynamicObject is MethodInfo)
                {
                    MethodInfo methodInfo = (MethodInfo)dynamicObject;
                    if (methodInfo.GetParameters().Length == 1)
                    {
                        methodInfo = methodInfo.DeclaringType.GetMethod(
                            "Set" + methodInfo.Name.Substring("Get".Length),
                            BindingFlags.Static | BindingFlags.Public);
                    }
                    propertyCanWrite = methodInfo != null && methodInfo.GetParameters().Length == 2;
                }
                else if (dynamicObject is EventInfo)
                {
                    propertyCanWrite = true;
                }
                else
                {
                    propertyCanWrite = false;
                }

                if (!propertyCanWrite)
                {
                    ThrowExceptionWithLine(SR.Get(SRID.ParserReadOnlyProp, attribLocalName));
                }
            }

            if (propInfo != null && !XamlTypeMapper.IsAllowedPropertySet(propInfo))
            {
                ThrowException(SRID.ParserCantSetAttribute, "property", declaringType.Name + "." + attribLocalName, "set");
            }

            string parentName = parentType != null ? parentType.Name : string.Empty;
            bool isRuntimeNameProperty = IsNameProperty(attribValue, parentName, attribLocalName, attribNamespaceURI,
                                                                                GetRuntimeNamePropertyName(parentType));
            Type propType;

            if (isRuntimeNameProperty)
            {
                propType = null;
                XamlTypeMapper.ValidateNames(attribValue, lineNumber, linePosition);
            }
            else
            {
                // Validate that enum values aren't pure digits.
                propType = XamlTypeMapper.GetPropertyType(dynamicObject);
                XamlTypeMapper.ValidateEnums(attribLocalName, propType, attribValue);
            }

            AttributeData data = _extensionParser.IsMarkupExtensionAttribute(declaringType,
                                             dynamicObjectName, ref attribValue,
                                             lineNumber, linePosition, depth, dynamicObject);

            if (data != null)
            {
                if (data.IsSimple)
                {
                    WritePropertyWithExtension(dynamicObjectName,
                                               dynamicObject,
                                               assemblyName,
                                               declaringTypeFullName,
                                               data.Args,
                                               data.ExtensionTypeId,
                                               data.IsValueNestedExtension,
                                               data.IsValueTypeExtension);
                }
                else
                {
                    // If the MarkupExtension expands into a complex property subtree, then
                    // defer handling of it until after all the properties have been processed.
                    if (markupExtensionList == null)
                    {
                        markupExtensionList = new ArrayList(1);
                    }
                    markupExtensionList.Add(data);
                }
            }
            else
            {
                BamlAttributeUsage attributeUsage = BamlAttributeUsage.Default;
                if (isRuntimeNameProperty)
                {
                    // Do not allow Name on abstract classes, classes without default constructors
                    // or value types.
                    if (parentType != null &&
                        (parentType.IsAbstract ||
                         parentType.GetConstructor(Type.EmptyTypes) == null ||
                         parentType.IsValueType))
                    {
                        ThrowException(SRID.ParserNoNameOnType, parentType.Name);
                    }
                    attributeUsage = BamlAttributeUsage.RuntimeName;
                }

#if PBTCOMPILER
                if (propType == ReflectionHelper.GetMscorlibType(typeof(Type)))
#else
                if (propType == typeof(Type))
#endif
                {
                    // if the property type is typeof(Type), we call WritePropertyWithExtension of the attribute
                    // value in order to convert the attribute value into a type as though it were a TypeExtension.
                    WritePropertyWithExtension(dynamicObjectName,
                                               dynamicObject,
                                               assemblyName,
                                               declaringTypeFullName,
                                               attribValue,
                                               (short)KnownElements.TypeExtension,
                                               false /*IsValueNestedExtension*/,
                                               false /*IsValueTypeExtension*/);
                }
                else
                {
                    WriteProperty(dynamicObjectName, dynamicObject, assemblyName, declaringTypeFullName,
                        attribValue, attributeUsage);
                }
            }
        }

        // Helper method to retrieve the Name of the property of a class that is referenced in
        // the RuntimeNamePropertyAttribute if any.
        private string GetRuntimeNamePropertyName(string parentTypeName, string parentTypeNamespace)
        {
            // If there is no type to look at, then we're done (e.g. this happens during WriteUnknownAttribute).
            if (parentTypeName == null)
            {
                return null;
            }

            Type parentType = XamlTypeMapper.GetType(parentTypeNamespace, parentTypeName);
            return GetRuntimeNamePropertyName(parentType);
        }

        // Helper method to retrieve the Name of the property of a class that is referenced in
        // the RuntimeNamePropertyAttribute if any.
        private string GetRuntimeNamePropertyName(Type objectType)
        {
            bool knownName = false;
            if (objectType != null)
            {
                bool isDefaultAsm = true;
                BamlAssemblyInfoRecord bairPF = XamlTypeMapper.MapTable.GetAssemblyInfoFromId(-1);
                if (bairPF.Assembly == null)
                {
                    isDefaultAsm = objectType.Assembly.FullName.StartsWith("PresentationFramework", StringComparison.OrdinalIgnoreCase);
                }
                else
                {
                    isDefaultAsm = objectType.Assembly == bairPF.Assembly;
                }

                if (isDefaultAsm)
                {
                    //if it's a subclass of a known Framwork type return the Name field to avoid reflection
                    if (KnownTypes.Types[(int)KnownElements.FrameworkElement].IsAssignableFrom(objectType) ||
                        KnownTypes.Types[(int)KnownElements.FrameworkContentElement].IsAssignableFrom(objectType) ||
                        KnownTypes.Types[(int)KnownElements.Timeline].IsAssignableFrom(objectType) ||
                        KnownTypes.Types[(int)KnownElements.BeginStoryboard].IsAssignableFrom(objectType))
                    {
                        return DefinitionRuntimeName;
                    }

                    knownName = true;
                }
                else
                {
#if PBTCOMPILER
                    Assembly asmPC = ReflectionHelper.GetAlreadyReflectionOnlyLoadedAssembly("PRESENTATIONCORE");
#else
                    Assembly asmPC = ReflectionHelper.GetAlreadyLoadedAssembly("PRESENTATIONCORE");
#endif
                    if (asmPC == null)
                    {
                        isDefaultAsm = objectType.Assembly.FullName.StartsWith("PresentationCore", StringComparison.OrdinalIgnoreCase);
                    }
                    else
                    {
                        isDefaultAsm = objectType.Assembly == asmPC;
                    }

                    if (isDefaultAsm)
                    {
                        if (KnownTypes.Types[(int)KnownElements.Timeline].IsAssignableFrom(objectType))
                        {
                            return DefinitionRuntimeName;
                        }

                        knownName = true;
                    }
                }

                if (!knownName)
                {
#if !PBTCOMPILER
                    object[] attributes = objectType.GetCustomAttributes(KnownTypes.Types[(int)KnownElements.RuntimeNamePropertyAttribute], true);

                    if (attributes != null)
                    {
                        for (int i = 0; i < attributes.Length; i++)
                        {
                            RuntimeNamePropertyAttribute rName = attributes[i] as RuntimeNamePropertyAttribute;
                            if (rName != null)
                            {
                                return rName.Name;
                            }
                        }
                    }
#else
                    string propName = null;
                    Type baseType = objectType;
                    Type attrType = KnownTypes.Types[(int)KnownElements.RuntimeNamePropertyAttribute];
                    Type TimelineType = KnownTypes.Types[(int)KnownElements.Timeline];
                    Type FEType = KnownTypes.Types[(int)KnownElements.FrameworkElement];
                    Type FCEType = KnownTypes.Types[(int)KnownElements.FrameworkContentElement];
                    Type BSBType = KnownTypes.Types[(int)KnownElements.BeginStoryboard];

                    // Keep looking up the base class hierarchy until an appropriate known base class
                    // with Name prop in PC or PF is reached.
                    while (null != baseType && null == propName)
                    {
                        propName = ReflectionHelper.GetCustomAttributeData(baseType, attrType, false);
                        if (propName == null)
                        {
                            baseType = baseType.BaseType;
                            if (baseType != null)
                            {
                                if (baseType.Assembly == XamlTypeMapper.AssemblyPF)
                                {
                                    if (FEType.IsAssignableFrom(baseType) ||
                                        FCEType.IsAssignableFrom(baseType) ||
                                        BSBType.IsAssignableFrom(baseType))
                                    {
                                        return DefinitionRuntimeName;
                                    }
                                }
                                else if (baseType.Assembly == XamlTypeMapper.AssemblyPC)
                                {
                                    if (TimelineType.IsAssignableFrom(baseType))
                                    {
                                        return DefinitionRuntimeName;
                                    }
                                }
                            }
                        }
                    }

                    return propName;
#endif
                }
            }

            return null;
        }

        //Helper method to determine if a given property is the Name property of an element
        private bool IsNameProperty(string attributeValue, string elementName, string attributeName, string attribNamespaceURI, string propertyName)
        {
            bool isName = false;

            //First check if there is a property with the RuntimeNameProperty attribute set
            if (!String.IsNullOrEmpty(propertyName))
            {
                isName = String.Equals(attributeName, propertyName);
            }
            //now check if it's an x:Name
            else
            {
                isName = String.Equals(attributeName, "Name") && String.Equals(attribNamespaceURI, DefinitionNamespaceURI);
            }

            if (isName && _definitionScopeType != null)
            {
                ThrowException(SRID.ParserNoNameUnderDefinitionScopeType, attributeValue, elementName, _definitionScopeType.Name);
            }

            return isName;
        }

        private bool WriteNameProperty(
            string attribLocalName,
            Type parentType,
            string attribValue,
            string parentTypeNamespace,
            HybridDictionary resolvedProperties,
            string propertyName)
        {
            if (parentType != null)
            {
                if (!String.IsNullOrEmpty(propertyName))
                {
                    PropertyInfo propertyMember = parentType.GetProperty(propertyName);
                    if (propertyMember != null)
                    {
                        CheckDuplicateProperty(resolvedProperties, propertyName, propertyMember);
                        string assembly = parentType.Assembly.FullName;
                        if (!String.IsNullOrEmpty(assembly))
                        {
                            WriteNameProperty(propertyName, propertyMember, assembly, parentType.FullName, attribValue, BamlAttributeUsage.RuntimeName);
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        // Return true if a given property is writable.
        private bool PropertyIsWriteable(object propertyMember, Type declaringType)
        {
            PropertyInfo propertyInfo = propertyMember as PropertyInfo;
            if (propertyInfo != null)
            {
                return propertyInfo.CanWrite;
            }

            MethodInfo methodInfo = propertyMember as MethodInfo;
            if (methodInfo != null)
            {
                return (methodInfo.GetParameters().Length == 2) ||
                    null != methodInfo.DeclaringType.GetMethod("Set" + methodInfo.Name.Substring("Get".Length),
                        BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);
            }

#if !PBTCOMPILER
            else if (propertyMember is DependencyProperty)
            {
                return !((DependencyProperty)propertyMember).ReadOnly;
            }
#endif
            return true; 
        }

        #endregion // Attributes

        #region Elements

        /*
        private void VerifyIListItemType( Type type )
        {
            return;
            /*
            if( ParentContext == null )
            {
                return;
            }

            Type listType = ParentContext.CollectionItemType;
            if( listType == null )
            {
                return;
            }

            if( !listType.IsAssignableFrom(type) )
            {
                throw new Exception( "Type mismatch" );
            }
        }
        */



        //
        // Handle a Baml tag, which is most elements and complex properties
        //

        private void CompileBamlTag(
                XmlNodeType xmlNodeType,      // Type of xml node we are processing.
            ref bool endTagHasBeenRead)   // true if we have already read past the end tag for this Element
        {
            Type elementBaseType = null;
            int depth = XmlReader.Depth;
            string typeFullName = null;
            string assemblyName = null;
            Type serializerType = null;
            string dynamicObjectName = null;
            bool resolvedTag;
            bool isEmptyElement = false;
            bool needToReadNextTag = true;
            string localName = XmlReader.LocalName;
            string prefix = XmlReader.Prefix;
            string namespaceURI = XmlReader.NamespaceURI;

            // Handle each type of xml node.
            try
            {
                switch (xmlNodeType)
                {
                    case XmlNodeType.Element:
                        {
                            Object dynamicObject = null;
                            isEmptyElement = XmlReader.IsEmptyElement;

                            // See if this is an object element or a property element.
                            bool hasDotInName = false;
                            if (localName.LastIndexOf('.') != -1)
                            {
                                hasDotInName = true;
                            }

                            // begin of XML island?
                            if (!isEmptyElement && IsXmlIsland(namespaceURI, localName))
                            {
                                if(_xmlDataIslandDepth != -1)
                                {
                                    ThrowException(SRID.ParserNoNestedXmlDataIslands);
                                }
                                _xmlDataIslandDepth = depth;

                                // consume and compile the xml island
                                CompileXmlIsland(depth);

                                _xmlDataIslandDepth = -1;
                                // CompileXmlIsland already consumed the ending x:XData tag
                                endTagHasBeenRead = true;
                                break;
                            }
                            // check if have a namespaceUri
                            if (null == namespaceURI)
                            {
                                // check if namespace-less tag could be part of a xml island that the parent element/property is expecting
                                // usually indicates the author missed to wrap the data island with <x:XData>
                                if (IsXmlIslandExpected())
                                    ThrowException(SRID.ParserXmlIslandMissing, GrandParentContext.ChildTagLocalName);
                                if (null == prefix)
                                {
                                    ThrowException(SRID.ParserNoNamespace, localName);
                                }
                                else
                                {
                                    ThrowException(SRID.ParserPrefixNSElement, prefix, localName);
                                }
                            }

                            // Scan for mapping URI protocols.
                            ScanForMappingProtocols();

                            // Is this an object element?
                            if (!hasDotInName)
                            {
                                // to put in the Tree. i.e. false is or should there be a flag for
                                // compiler, .net, etc.
                                resolvedTag = GetElementType(false, localName, namespaceURI, ref assemblyName,
                                    ref typeFullName, ref elementBaseType, ref serializerType);

                                if (resolvedTag)
                                {
                                    // update stackData with the elementBaseType
                                    CurrentContext.ContextData = elementBaseType;

                                    // Set context type
                                    CurrentContext.ContextType = ElementContextType.Default;

                                    //VerifyIListItemType(elementBaseType);

                                    CompileElement(assemblyName, typeFullName, depth, serializerType,
                                        namespaceURI, isEmptyElement, ref needToReadNextTag);
                                }
                                else
                                {
                                    // This prevents conditions where ResourceDictionary is followed by a locally defined type,
                                    // the ParentContext needs to know that FirstChildRead is true, else there is a mismatch in
                                    // StartElement & EndElement nodes. 
                                    if (ParentContext != null && !namespaceURI.Equals(DefinitionNamespaceURI))
                                    {
                                        ParentContext.FirstChildRead = true;
                                    }
                                }
                            }
                            else  // Otherwise, this may be a property element
                            {
                                Type declaringType = null;
                                string ownerName;
                                string propName;
                                SplitPropertyElementName(localName, out ownerName, out propName);
                                resolvedTag = GetPropertyComplex(ownerName, propName, namespaceURI, ref  assemblyName,
                                     ref typeFullName, ref dynamicObjectName, ref  elementBaseType,
                                     ref  dynamicObject, ref declaringType);
                                if (resolvedTag)
                                {
                                    // update stackData with the elementBaseType
                                    CurrentContext.ContextData = elementBaseType;

                                        if (KnownTypes.Types[(int)KnownElements.RoutedEvent].IsAssignableFrom(elementBaseType))
                                        {
                                            ThrowException(SRID.ParserNoEventTag, localName);
                                        }
                                        else   // Its not an event, so it must be a property of some kind.
                                        {
                                            // Get the type of the property from the PropertyInfo, MethodInfo
                                            // or DependencyProperty.  Then use the name of that property type
                                            // as a key when looking up the serializer.  This is a bit
                                            // redundant, but at the moment the type and serializer cache is
                                            // keyed by string name, not by type...
                                            Type propertyType = XamlTypeMapper.GetPropertyType(dynamicObject);

                                            // Use the property also while querying for the serializer because some
                                            // properties may be associated with a XamlSerializer
                                            // and we want't to respect that. NOTE: The GetTypeAndSerializer call updates
                                            // the cache etc, and the GetSerializerType gets us the best matching serializer.
                                            // Property attributes being of higher priority than type attributes.
                                            TypeAndSerializer typeAndSerializer =
                                                XamlTypeMapper.GetTypeAndSerializer(namespaceURI, propertyType.Name, dynamicObject);
                                            Debug.Assert(typeAndSerializer == null ||
                                                         typeAndSerializer.SerializerType == null ||
                                                         propertyType == typeAndSerializer.ObjectType);
                                            serializerType = typeAndSerializer != null ? typeAndSerializer.SerializerType : null;

                                            CompileComplexProperty(dynamicObject, propertyType, serializerType,
                                                    depth, assemblyName, typeFullName, dynamicObjectName,
                                                    localName, declaringType, namespaceURI, out needToReadNextTag,
                                                    ref endTagHasBeenRead);
                                    }
                                }
                            }


                            // If still unresolved, this may be a special Xaml tag (x:)
                            if (!resolvedTag && namespaceURI.Equals(DefinitionNamespaceURI))
                            {
                                CurrentContext.ContextType = ElementContextType.DefTag;
                                CompileDefTag(xmlNodeType, ref endTagHasBeenRead);
                                return;
                            }

                            // If still haven't found an Element or ComplexProperty then it is
                            // currently unknown, so make an UnknownTag record and store the
                            // tags attributes as UnknownAttribute records.

                            if (!resolvedTag)
                            {
                                // check if unknown tag could be part of a xml island that the parent element/property is expecting
                                // usually indicates the author missed to wrap the data island with <x:XData>
                                if (IsXmlIslandExpected())
                                    ThrowException(SRID.ParserXmlIslandMissing, GrandParentContext.ChildTagLocalName);
                                CurrentContext.ContextType = ElementContextType.Unknown;
                                CurrentContext.ContextData = new UnknownData(localName, namespaceURI);
                                WriteUnknownTagStart(namespaceURI, localName, depth);
                                WriteAttributes(null, namespaceURI, localName, depth);
                            }
                            // review, currently no way to attach the "attributes" on structured
                            // properties. probably need a way to hook this up but for now just read the
                            // inner and treat as attribute value.
                            break;
                        }
                    case XmlNodeType.EndElement:
                        {
                            if (_definitionScopeType != null && _definitionScopeType.Equals(CurrentContext.ContextData))
                            {
                                _definitionScopeType = null;
                            }

                            // If we've faked a start tag, then we need to fake an end tag also.  This
                            // occurs in the case where a complex property tag was encountered without
                            // an object tag directly below it.
                            if (CurrentContext.NeedToWriteEndElement)
                            {
                                Debug.Assert(CurrentContext.ContextType == ElementContextType.PropertyComplex ||
                                             CurrentContext.ContextType == ElementContextType.PropertyIList ||
                                             CurrentContext.ContextType == ElementContextType.PropertyIDictionary,
                                        "NeedToWriteEndElement set on invalid ContextType");

                                WriteElementEnd();
                            }

                            switch (CurrentContext.ContextType)
                            {
                                case ElementContextType.PropertyComplex:
                                    WritePropertyComplexEnd();
                                    break;

                                case ElementContextType.PropertyArray:
                                    WritePropertyArrayEnd();
                                    break;

                                case ElementContextType.PropertyIList:
                                    WritePropertyIListEnd();
                                    break;

                                case ElementContextType.PropertyIDictionary:
                                    WritePropertyIDictionaryEnd();
                                    break;

                                case ElementContextType.Unknown:
                                    WriteUnknownTagEnd();
                                    break;

                                default:
                                    WriteElementEnd();
                                    break;
                            }
                        }
                        break;

                    case XmlNodeType.CDATA:
                    case XmlNodeType.Text:
                    case XmlNodeType.SignificantWhitespace:
                    case XmlNodeType.Whitespace:
                        {
                            //Treat consecutive text, cdata, etc... as the same textnode
                            TextFlowStackData textFlowData3 = (TextFlowStackData)_textFlowStack.Peek();
                            textFlowData3.EncounteredIgnorableTag = true;

                            CompileText(XmlReader.NodeType, XmlReader.Value);
                        }
                        break;

                    case XmlNodeType.ProcessingInstruction:
                        CompilePI();
                        TextFlowStackData textFlowData1 = (TextFlowStackData)_textFlowStack.Peek();
                        // Ignore comments when processing text content
                        textFlowData1.EncounteredIgnorableTag = true;
                        break;

                    case XmlNodeType.Comment:
                        TextFlowStackData textFlowData2 = (TextFlowStackData)_textFlowStack.Peek();
                        // Ignore comments when processing text content
                        textFlowData2.EncounteredIgnorableTag = true;
                        break;

                    // Custom entity references are not currently supported.
                    case XmlNodeType.EntityReference:
                        ThrowException(SRID.ParserEntityReference, localName);
                        break;

                    default:
                        // unknown node type.  We just skip over.
                        break;
                }
            }
            finally
            {
                // This needs to get cleaned up. It is up to this function to move the reader
                // to the next position. However, there are cases that the code above
                // has already moved the read to the proper posiiton
                // endTagHasBeenRead - We have already read past the end tag for this Element
                // isEmptyElement - This is an EmptyElement tag, don't read  since the
                //       next call to this funciton will be an EndElement call which will
                //       move the position
                // needToReadNextTag - boolean set to false if the above code is already at the
                //       next tag that should be processed.
                if (!endTagHasBeenRead && !isEmptyElement && needToReadNextTag)
                {
                    XmlReader.Read();
                }
            }
        }

        // Returns true if current element is beginning of an XML island.
        private bool IsXmlIsland(string namespaceUri, string tagName)
        {
            if (ParentContext != null)
            {
                // a XML data island must be wrapped in <x:XData>
                if( tagName == DefinitionXDataTag &&
                    namespaceUri == DefinitionNamespaceURI)
                {
                    if (!IsXmlIslandExpected())
                        ThrowException(SRID.ParserXmlIslandUnexpected, GrandParentContext.ChildTagLocalName);
                    return true;
                }
            }
            return false;
        }

        // Returns true if an XML island is expected in place of an element.
        private bool IsXmlIslandExpected()
        {
            if (ParentContext != null)
            {
                // check if ParentContext can accept a Xml island:
                // either its ContentProperty is capable of parsing literal Xml content via IXmlSerializable...
                if (ParentContext.IsContentPropertySet)
                {
                    PropertyInfo pi = ParentContext.ContentPropertyInfo;

                    return IsAssignableToIXmlSerializable(pi.PropertyType);
                }
                // ...or a explicit complex property that can parse literal Xml content
                else if (ParentContext.ContextType == ElementContextType.PropertyComplex)
                {
                    return IsAssignableToIXmlSerializable(ParentContext.ContextDataType);
                }
            }
            return false;
        }

        // Called when a content property supports IXmlSerializable.
        // Writes a LiteralContent node to the BAML stream and is
        // treated as an XML island by BamlRecordReader.
        private void CompileXmlIsland(int depth)
        {
            // InnerXml also consumes the ending x:XData tag
            string text = XmlReader.ReadInnerXml();
            if (text != null)
                text = text.Trim();
            if (!string.IsNullOrEmpty(text))
            {
                WriteLiteralContent(text, depth, LineNumber, LinePosition);
            }
        }

        private void CheckAllowedProperty(PropertyInfo pi, string propName)
        {
            if (pi != null)
            {
                if (pi.CanWrite)
                {
                    if (!XamlTypeMapper.IsAllowedPropertySet(pi))
                    {
                        ThrowException(SRID.ParserCantSetAttribute, "property", propName, "set");
                    }
                }

                if (!XamlTypeMapper.IsAllowedPropertyGet(pi))
                {
                    ThrowException(SRID.ParserCantGetProperty, propName);
                }
            }
        }

        // Handle a complex property on a CLR or dependency object.  Thas the
        // form <Classname.PropertyName> in XAML
        //
        // NOTE:  The order this is checked in must the same order as the
        //        BamlWriter.WriteStartComplexProperty so that we have
        //        the same property behavior when loading from Xaml and Baml
        private void CompileComplexProperty(
                object propertyMember,      // PropertyInfo, DependencyProperty or
            // MethodInfo for static setter
                Type propertyType,        // Type of the property
                Type serializerType,      // Type of serializer for the property
                int depth,
                string assemblyName,        // Assembly of the owner of the property
                string typeFullName,        // Type name of the owner or the property
                string dynamicObjectName,
                string complexPropName,
                Type declaringType,       // Actual type that corresponds to typeFullName
                string namespaceURI,
            out bool needToReadNextTag,
            ref bool endTagHasBeenRead)
        {
            int lineNumber = LineNumber;
            int linePosition = LinePosition;

            needToReadNextTag = true;
            endTagHasBeenRead = false;

            // Complex properties are not allowed under other complex property tags
            // Note that this check is not foolproof - the ParentContext may not be
            //  available at this time (we'll get null) or it might not be an
            //  ElementContextStackData as expected (the 'as' will turn it null.)
            // BamlRecordReader has secondary protection against nested property
            //  records, but the error message less friendly to users. ("'Property'
            //  record unexpected in BAML stream.")
            ElementContextStackData parentTag = ElementContextStack.ParentContext as ElementContextStackData;
            if( parentTag != null )
            {
                if ( parentTag.ContextType == ElementContextType.PropertyComplex ||
                     parentTag.ContextType == ElementContextType.PropertyArray ||
                     parentTag.ContextType == ElementContextType.PropertyIList ||
                     parentTag.ContextType == ElementContextType.PropertyIDictionary )
                {
                    ThrowException(SRID.ParserNestedComplexProp, complexPropName);
                }
            }

            // No attribute specifications except x:Uid are allowed on a complex property tag
            int attributeCount = XmlReader.AttributeCount;
            if ( attributeCount > 0 )
            {
                int attributesToIgnore = 0;

                if( XmlReader.GetAttribute(DefinitionUid, DefinitionNamespaceURI) != null)
                {
                    // Ignore the x:Uid attribute that's been inserted all over the place
                    attributesToIgnore++;
                }

                if( attributeCount > attributesToIgnore )
                {
                    ThrowException(SRID.ParserNoPropOnComplexProp);
                }
            }

            // Remember the child tag type, in case it is needed for error reporting.
            ParentContext.SetChildTag(dynamicObjectName);
            ParentContext.ChildPropertyType = propertyType;

            /*
            if( typeof(IEnumerable).IsAssignableFrom(propertyType) )
            {
                Type iListTArgument = propertyType.GetInterface("System.Collections.Generic.ICollection`1");
                if( iListTArgument != null )
                {
                    CurrentContext.CollectionItemType = iListTArgument.GetGenericArguments()[0];
                }
            }
            */

            //Since we've now hit a PropertyElement advance the Content state machine.
            ContentPropertySeesAProperty(ParentContext);

            // Determine if the property is writable
            bool propertyCanWrite = PropertyIsWriteable(propertyMember, declaringType);
            PropertyInfo pi = propertyMember as PropertyInfo;
            BamlRecordType recordType = BamlRecordManager.GetPropertyStartRecordType(propertyType, propertyCanWrite);
            // Check the type of the property and use that to determine what type of
            // property node to create.
            switch (recordType)
            {
                case BamlRecordType.PropertyArrayStart:
                {
                    // If the property is not writable, then complain.
                    if (ControllingXamlParser != null &&
                        !propertyCanWrite)
                    {
                        ThrowExceptionWithLine(SR.Get(SRID.ParserReadOnlyProp,
                                               dynamicObjectName));
                    }

                    if (pi != null && !XamlTypeMapper.IsAllowedPropertySet(pi))
                    {
                        ThrowException(SRID.ParserCantSetAttribute, "property", complexPropName, "set");
                    }

                    CompileComplexPropertyArray(
                                propertyMember,
                                propertyType,
                                depth,
                                assemblyName,
                                typeFullName,
                                dynamicObjectName);
                    break;
                }
                case BamlRecordType.PropertyIDictionaryStart:
                {
                    CheckAllowedProperty(pi, complexPropName);

                    CompileComplexPropertyIDictionary(
                                propertyMember,
                                propertyType,
                                serializerType,
                                depth,
                                assemblyName,
                                typeFullName,
                                dynamicObjectName);
                    break;
                }
                default: // PropertyIListStart, PropertyComplexStart
                {
                    bool isList = (recordType == BamlRecordType.PropertyIListStart);

                    // If the property is not writable, then complain unless it is IXmlSerializable
                    if (ControllingXamlParser != null &&
                        !propertyCanWrite &&
                        !isList &&
                        !IsAssignableToIXmlSerializable(propertyType))
                    {
                        ThrowExceptionWithLine(SR.Get(SRID.ParserReadOnlyProp,
                                               dynamicObjectName));
                    }

                    // make sure a settable property is actually allowed to be set
                    if (pi != null && !propertyCanWrite && !XamlTypeMapper.IsAllowedPropertyGet(pi))
                    {
                        ThrowException(SRID.ParserCantGetProperty, complexPropName);
                    }
                    if (pi != null && propertyCanWrite && !XamlTypeMapper.IsAllowedPropertySet(pi))
                    {
                        ThrowException(SRID.ParserCantSetAttribute, "property", complexPropName, "set");
                    }

                    string textValue = null;
                    bool endTagReached = false;
                    bool isEmptyElement = XmlReader.IsEmptyElement;

                    // Determining if a property is complex can advance the XmlReader, so
                    // remember the position for the complex property before the reader is advanced.
                    // If the property is read-only, then we had a List, and read-only lists cannot
                    // be treated as simple, so no need to check if the property is complex.
                    bool isComplex = !propertyCanWrite || DetermineIfPropertyComplex(isEmptyElement,out textValue, out endTagReached);

                    if (isComplex)
                    {
                        if (isList)
                        {
                            CompileComplexPropertyIList(
                                        propertyMember,
                                        propertyType,
                                        serializerType,
                                        depth,
                                        assemblyName,
                                        typeFullName,
                                        dynamicObjectName);
                        }
                        else
                        {
                            CompileComplexPropertySingle(
                                        lineNumber,
                                        linePosition,
                                        propertyMember,
                                        propertyType,
                                        depth,
                                        assemblyName,
                                        typeFullName,
                                        dynamicObjectName);
                        }

                        // if got some text then write it out.
                        if (null != textValue)
                        {
                            CompileText(XmlNodeType.None, textValue);
                        }
                        else if (endTagReached)
                        {
                           // If the end tag has been reached, but there is no text, then this
                           // is an empty complex property tag.  Complain about that.
                           ThrowException(SRID.ParserEmptyComplexProp, complexPropName);
                        }

                        // If property is complex, then might have gotten here if either we had
                        // a read-only List property (as per isList) or because DetermineIfPropertyComplex
                        // returned true.  In the former case we need to read the next tag, in the latter
                        // we do not.
                        needToReadNextTag = !propertyCanWrite;
                    }
                    else
                    {
                        // would have read past the end tag or its an empty element.
                        endTagHasBeenRead = true;

                        if (null == textValue)
                        {
                            ThrowException(SRID.ParserPropNoValue, dynamicObjectName);
                        }

                        Debug.Assert(null != assemblyName, "property without an AssemblyName");
                        Debug.Assert(null != typeFullName, "property without a type name");
                        Debug.Assert(null != dynamicObjectName, "property without a field Name");

                        WriteComplexAsSimpleProperty(
                                 dynamicObjectName,
                                 namespaceURI,
                                 propertyMember,
                                 assemblyName,
                                 declaringType, typeFullName,
                                 textValue,
                                 BamlAttributeUsage.Default);
                    }
                    break;
                }
            }
        }

        // Write Xaml nodes corresponding to a complex property that has either
        //  expecting a single value or an IList complex property that expects
        //  multiple values.
        private void CompileComplexPropertySingle(
                int    lineNumber,
                int    linePosition,
                object propertyMember,      // PropertyInfo, DependencyProperty or
            // MethodInfo for static setter
                Type propertyType,        // Type of the property
                int depth,
                string assemblyName,        // Assembly of the owner of the property
                string typeFullName,        // Type name of the owner or the property
                string dynamicObjectName)
        {
            // If found a Property as a tag then switch context
            // over to a PropertyComplex and set Type to the property info's type.
            CurrentContext.ContextType = ElementContextType.PropertyComplex;
            CurrentContext.ContextData = propertyType;

            WritePropertyComplexStart(depth, lineNumber, linePosition, propertyMember,
                                      assemblyName, typeFullName, dynamicObjectName,
                                      ParentProperties);
        }

        // Write Xaml nodes corresponding to a complex property that supports IList or IAddChild
        private void CompileComplexPropertyIList(
                object propertyMember,      // PropertyInfo, DependencyProperty or
            // MethodInfo for static setter
                Type propertyType,        // Type of the property
                Type serializerType,
                int depth,
                string assemblyName,        // Assembly of the owner of the property
                string typeFullName,        // Type name of the owner or the property
                string dynamicObjectName)
        {
            // We've found a ReadOnly IList, IEnumerable or IAddChild property, or.
            // Treat this as special so that we don't create an instance of the property but
            // rather use the existing value to add children (for IList and IAddChild), or
            // the existing parent to add children (for IEnumerable)

            // For IEnumerables that are not ILists, make sure the parent supports IAddChild.
            if (ControllingXamlParser.StrictParsing &&
#if PBTCOMPILER
                 ReflectionHelper.GetMscorlibType(typeof(IEnumerable)).IsAssignableFrom(propertyType) &&
                 !ReflectionHelper.GetMscorlibType(typeof(IList)).IsAssignableFrom(propertyType) &&
#else
                 typeof(IEnumerable).IsAssignableFrom(propertyType) &&
                 !typeof(IList).IsAssignableFrom(propertyType) &&
#endif
                (ParentContext == null ||
                 ParentContext.ContextType != ElementContextType.Default ||
                 !BamlRecordManager.TreatAsIAddChild(ParentContext.ContextDataType)))
            {
                ThrowException(SRID.ParserIEnumerableIAddChild,
                               dynamicObjectName,
                               ParentContext.ContextData.ToString());
            }

            CurrentContext.ContextType = ElementContextType.PropertyIList;
            CurrentContext.ContextData = propertyType.GetElementType();
            WritePropertyIListStart(depth, propertyMember, assemblyName,
                                        typeFullName, dynamicObjectName);
        }

        // Write Xaml nodes corresponding to a complex property that is an IDictionary
        private void CompileComplexPropertyIDictionary(
                object propertyMember,      // PropertyInfo, DependencyProperty or
            // MethodInfo for static setter
                Type propertyType,        // Type of the property
                Type serializerType,      // Type of serializer for the property
                int depth,
                string assemblyName,        // Assembly of the owner of the property
                string typeFullName,        // Type name of the owner or the property
                string dynamicObjectName)
        {
            // We've found an IDictionary property.  Treat this as special
            // so that we don't create an instance of the property but rather
            // use the existing value to add children (if there is one).
            // Note that children (except for style, which is YASC) must
            // have a x:Key to provide a key
            CurrentContext.ContextType = ElementContextType.PropertyIDictionary;
            CurrentContext.ContextData = new DictionaryContextData(propertyType);
            WritePropertyIDictionaryStart(depth, propertyMember, assemblyName,
                                        typeFullName, dynamicObjectName);
            // We may or may not have an element tag come next in the xaml file that
            // is used to populate the dictionary property, or serve as a placeholder
            // if the dictionary property is read-only and has a value.  To make things
            // consistent downstream, always generate a Dictionary element tag now.
            string startElementAssemblyName = propertyType.Assembly.FullName;
            WriteElementStart(startElementAssemblyName, propertyType.FullName,
                            depth, propertyType, serializerType, true /*isInjected*/);

            // Since a dictionary element is being explicitly injected and it cannot be created
            // via the TypeConverter syntax, we can reset the state now so that the token reader
            // does not look ahead.
            TokenReaderNodeCollection.ResetTypeConverterDecision();

            WriteEndAttributes(depth, false);

            // Remember that the end element needs to get set.
            CurrentContext.NeedToWriteEndElement = true;
            _readAnotherToken = true;
        }

        // Write Xaml nodes corresponding to a complex property that is an array
        private void CompileComplexPropertyArray(
                object propertyMember,      // PropertyInfo, DependencyProperty or
            // MethodInfo for static setter
                Type propertyType,        // Type of the property
                int depth,
                string assemblyName,        // Assembly of the owner of the property
                string typeFullName,        // Type name of the owner or the property
                string dynamicObjectName)
        {
            if (XmlReader.AttributeCount > 0)
            {
                // The tag for a complex property array is not supposed to
                //  have any attributes.  (Meaningless in the array context.)
                //  We have some exceptions to the rule, check for them here.
                bool invalidAttributeFound = false;

                while (XmlReader.MoveToNextAttribute() &&
                        !invalidAttributeFound)
                {
                    if (XmlReader.LocalName == DefinitionUid &&
                        XmlReader.NamespaceURI == DefinitionNamespaceURI)
                    {
                        // An attribute we understand at this point is the
                        //  x:Uid attribute that's been appended to every XML tag
                        //  in the XAML.  We "understand" it here in the
                        //  sense that we won't throw an exception for it.
                        // We don't actually do anything about it here.
                    }
                    // else if( [Other acceptable condition] )
                    // {
                    //      Do something if needed
                    // }
                    else
                    {
                        invalidAttributeFound = true;
                    }
                }

                if (invalidAttributeFound)
                {
                    ThrowException(SRID.ParserNoAttrArray);
                }
            }
            // if this is an Array change the context to ClrArray.
            CurrentContext.ContextType = ElementContextType.PropertyArray;
            // Set context data to the type required by this array
            CurrentContext.ContextData = propertyType.GetElementType();
            WritePropertyArrayStart(depth, propertyMember, assemblyName,
                                    typeFullName, dynamicObjectName);
        }


        // An element tag of the type <ClassName ... > has been encountered.  This is either
        // a CLR or DependencyObject, so based on the interfaces ClassName implements and the
        // current content, process this tag.
        private void CompileElement(
                string assemblyName,
                string typeFullName,
                int depth,
                Type serializerType,
                string namespaceURI,
                bool isEmptyElement,
            ref bool needToReadNextTag)
        {
            if (null != ParentContext)
            {
                // Remember the child tag type, in case it is needed for error reporting.
                ParentContext.SetChildTag(typeFullName);

                ValidateCompileElementContextType();
            }

            CurrentContext.ContextType = ElementContextType.Default;
            Type currentObjectType = CurrentContext.ContextDataType;

            //Check to make sure that content is not split up by property elements.

            //Don't need to check this if we are compiling the root element.
            if (ParentContext != null)
            {
                // Verify... "throw"s if we are "After" Content and
                // returns false if there are multiple elements but
                // the property is not a container.
                if (!VerifyContentPropertySeesAnElement(ParentContext))
                {
                    //We need to error, do work to give a good error message.
                    string FirstTagName;

                    if (ParentContext.ContextDataType == null)
                    {
                        //PropertyElement was the Parent tag.
                        FirstTagName = ((Type)GrandParentContext.ContextData).Name + "." + GrandParentContext.ChildTagLocalName;
                    }
                    else
                    {
                        //ObjectElement was the Parent tag.
                        FirstTagName = ParentContext.ContextDataType.Name;
                    }

                    throw new InvalidOperationException(SR.Get(SRID.ParserCanOnlyHaveOneChild,
                        FirstTagName /* Parent or PropertyElement*/,
                        CurrentContext.ContextDataType.Name /* Child */));
                }
            }

            // If we previously synthesised an ElementStart and we now really have
            // a true element tag as the first child tag, then remove the previously synthesised node from
            // the reader's node collection, and mark the current context as not
            // needing to synthesis an end tag.
            // NOTE: We will only remove the synthesised node if it is a match for
            //       for the one specified in the actual XAML.  We consider it a match
            //       for the property class if it is a subclass of the property type
            if (ParentContext != null &&
                ParentContext.ContextData != null &&
                ParentContext.NeedToWriteEndElement &&
                !ParentContext.FirstChildRead)
            {

                ParentContext.FirstChildRead = true;
                if (((ParentContext.ContextDataType).IsAssignableFrom(currentObjectType) ||
                      KnownTypes.Types[(int)KnownElements.MarkupExtension].IsAssignableFrom(currentObjectType))
                              &&
                    (ParentContext.ContextType == ElementContextType.PropertyComplex ||
                     ParentContext.ContextType == ElementContextType.PropertyIDictionary ||
                     ParentContext.ContextType == ElementContextType.PropertyIList))
                {
                    TokenReaderNodeCollection.RemoveLastElement();   // EndAttributes
                    TokenReaderNodeCollection.RemoveLastElement();   // ElementStart
                    ParentContext.NeedToWriteEndElement = false;

                    // Remove any text flow stack alterations made as part of the
                    //  synthesized StartElement.  (This code came from
                    //  AddNodeToCollection for cases EndClrObject/EndElement.)
                    TextFlowStackData textFlowData = (TextFlowStackData)_textFlowStack.Peek();
                    if (textFlowData.InlineCount > 0)
                    {
                        textFlowData.InlineCount--;
                    }
                    else
                    {
                        TextFlowStack.Pop();
                    }
                }
                else
                {
                    // The fake element was not removed, so check that it can actually be
                    // created.  If this is a complex property and the fake element is not
                    // a concrete class with a default constructor, then it is an error.
                    if (ParentContext.ContextType == ElementContextType.PropertyComplex &&
                        (ParentContext.ContextDataType.IsAbstract ||
                         ParentContext.ContextDataType.GetConstructor(Type.EmptyTypes) == null))
                    {
                        ThrowException(SRID.ParserBadChild, ParentContext.ChildTagLocalName,
                                       GrandParentContext.ChildTagLocalName);
                    }
                }
            }

            if (ParentContext != null)
                ParentContext.FirstChildRead = true;  // may already be true

            WriteElementStart(assemblyName, typeFullName, depth,
                          currentObjectType, serializerType, false /*isInjected*/);

            CurrentContext.ContextData = currentObjectType;
            CurrentContext.NamespaceUri = namespaceURI;

            WriteAttributes(CurrentContext.ContextDataType, namespaceURI,
                            null, depth);

            if (!isEmptyElement)
                LoadContentPropertyInfo(currentObjectType);
        }

        #region ContentProperty

        private enum ParsingContent { Before, During, After };

        // VerifyContentPropertySeesAnElement drives the "Contiguous Content" state machine.
        // All Content must be together with no property elements interruping the
        //  list of content elements.
        // This method will throw on interrupted groups of content.  But in the
        //  case of a Content Property that is not a collection it will return
        //  false to the caller; who is in a position to generate a better error.
        private bool VerifyContentPropertySeesAnElement(ElementContextStackData context)
        {
            // If this is the first element then all is good.
            if (context.ContentParserState == ParsingContent.Before)
            {
                context.ContentParserState = ParsingContent.During;
                return true;
            }
            // if this is the 2nd or later elements the Property must be a container
            else if (context.ContentParserState == ParsingContent.During)
            {
                return context.IsContentPropertyACollection;
            }
            // if we are done added content then adding content is now an error.
            else  // if (context.ContentParserState == ParsingContent.During)
            {
                ThrowException(SRID.ParserContentMustBeContiguous);
                return false;  // compiler demands this.
            }
        }

        // ContentPropertySeesAProperty drives the "Contiguous Content" state machine.
        // on property elements move from state During to state After.
        private void ContentPropertySeesAProperty(ElementContextStackData context)
        {
            if (context.ContentParserState == ParsingContent.During)
            {
                context.ContentParserState = ParsingContent.After;
            }
        }

        private bool ShouldImplyContentProperty()
        {
            // If this (the parent) is an Element (not a property of some kind)
            // Default means "element"
            if (ElementContextType.Default != CurrentContext.ContextType)
                return false;

            // If we have already seen content then it has been taken care of.
            if (CurrentContext.ContentParserState == ParsingContent.During)
                return false;

            // and the curent XML element (the child) is not a property element.
            string localName = XmlReader.LocalName;
            if (localName.LastIndexOf('.') != -1)
                return false;

            // and this (the parent) isn't an IAddChildInternal using type.
            Type parentElementType = CurrentContext.ContextDataType;
            if (BamlRecordManager.TreatAsIAddChild(parentElementType))
                return false;

            // check for IList and IDictionary
#if PBTCOMPILER
            if (ReflectionHelper.GetMscorlibType(typeof(IList)).IsAssignableFrom(parentElementType)
                && ReflectionHelper.GetMscorlibType(typeof(IDictionary)).IsAssignableFrom(parentElementType))
#else
            if (typeof(IList).IsAssignableFrom(parentElementType)
                && typeof(IDictionary).IsAssignableFrom(parentElementType))
#endif
            {
                return false;
            }

            // and this (the parent) has a Content Property defined.
            if (!CurrentContext.IsContentPropertySet)
                return false;

            return true;
        }


        private void LoadContentPropertyInfo(Type elementType)
        {
            string namespaceUri  = CurrentContext.NamespaceUri;

            // Optimize if the type is an IAddChildInternal using type.
            // and don't load the ContenPropertyInfo.
            if (BamlRecordManager.TreatAsIAddChild(elementType))
                return;

            // Can't load if there is no contentPropertyDefined.
            string contentPropertyName = GetContentPropertyName(elementType);
            if (null == contentPropertyName)
                return;

            string propertyAssemblyName;
            object propertyDynamicObject;
            Type propertyDeclaringType;

            ResolveContentProperty(contentPropertyName, elementType, namespaceUri,
                        out propertyAssemblyName, out propertyDynamicObject, out propertyDeclaringType);

            CurrentContext.ContentPropertyInfo = (PropertyInfo)propertyDynamicObject;
            CurrentContext.ContentPropertyName = contentPropertyName;

            // if content property needs to be set, TypeConverter syntax for element creation is not
            // required. So we can reset the state now so that the token reader does not look ahead.
            /*
            if (ShouldImplyContentProperty())
            {
                TokenReaderNodeCollection.ResetTypeConverterDecision();
            }
             */

            return;
        }


        private void ResolveContentProperty(string contentPropertyName,
                        Type elementType,
                        string namespaceUri,
                        out string propertyAssemblyName,
                        out object propertyDynamicObject,
                        out Type propertyDeclaringType)
        {
            propertyDynamicObject = null;
            propertyAssemblyName = null;
            propertyDeclaringType = null;

            string propertyTypeFullName = null;
            string propertyDynamicObjectName = null;
            Type propertyBaseType = null;

            // Push a frame for GetPropertyComplex()
            ElementContextStackData elementContextStackData = new ElementContextStackData();
            elementContextStackData.ContextType = ElementContextType.Default;
            ElementContextStack.Push(elementContextStackData);

            bool resolved = GetPropertyComplex(elementType.Name, contentPropertyName, namespaceUri,
                ref propertyAssemblyName,      ref propertyTypeFullName,
                ref propertyDynamicObjectName, ref propertyBaseType,
                ref propertyDynamicObject,     ref propertyDeclaringType);

            ElementContextStack.Pop();

            PropertyInfo pi = propertyDynamicObject as PropertyInfo;

            if (!resolved  /* resolution was unsuccessful */
                ||
                pi == null /* resolved object is not a PropertyInfo */)
            {
                ThrowException(SRID.ParserInvalidContentPropertyAttribute, elementType.FullName,
                    contentPropertyName);
            }

            // check to see if content property is accessible\allowed.
            bool allowed = true;

#if PBTCOMPILER
            if (ReflectionHelper.GetMscorlibType(typeof(IList)).IsAssignableFrom(pi.PropertyType))
#else
            if (typeof(IList).IsAssignableFrom(pi.PropertyType))
#endif
            {
                allowed = XamlTypeMapper.IsAllowedPropertyGet(pi);
            }
            else if (!IsAssignableToIXmlSerializable(pi.PropertyType))
            {
                allowed = XamlTypeMapper.IsAllowedPropertySet(pi);
            }

            // We will resolve the content Property even for IAddChild using types
            // because we need to match IAddChild Content against Content Property "content"
            // at "check duplicate property usage" time.
            // This is made more difficult because of the IAddChild using types (FixedDocument.Pages
            // DocumentSequence.References) are non-IList collections and thus fail the above
            // allowed check.   But it is OK if we are IAddChild because we won't be using the
            // content property anyway.
            if (!allowed && !BamlRecordManager.TreatAsIAddChild(elementType))
            {
                ThrowException(SRID.ParserCantSetContentProperty, contentPropertyName, elementType.Name);
            }
        }


        private void CompileContentProperty(ElementContextStackData context)
        {
            int depth = XmlReader.Depth;
            object contentPropertyInfo = context.ContentPropertyInfo;
            Type propertyDeclaringType = XamlTypeMapper.GetDeclaringType(contentPropertyInfo);
            string propertyAssemblyName = propertyDeclaringType.Assembly.FullName;
            string contentPropertyName = context.ContentPropertyName;

            WriteContentProperty(depth, LineNumber, LinePosition, contentPropertyInfo,
                propertyAssemblyName, propertyDeclaringType.FullName, contentPropertyName);
        }
        #endregion ContentProperty

        private void ValidateCompileElementContextType()
        {
            switch (ParentContext.ContextType)
            {
                case ElementContextType.Default:
                case ElementContextType.Unknown:
                    {
                        // Check whether this object can be added to the parent, and throw
                        // an exception if the parent does not support an appropriate add
                        // interface.  Note that if we don't have an associated XamlParser,
                        // then this rule may not apply, so don't complain.  Leave it up to
                        // whatever is using the XamlReaderHelper to decide.
                        Type parentType = ParentContext.ContextDataType;
                        if (parentType != null &&
                            ControllingXamlParser.StrictParsing &&
                            !ParentContext.IsContentPropertySet &&
                            !BamlRecordManager.TreatAsIAddChild(parentType) &&
#if PBTCOMPILER
                            !ReflectionHelper.GetMscorlibType(typeof(IEnumerable)).IsAssignableFrom(parentType) &&
                            !ReflectionHelper.GetMscorlibType(typeof(IList)).IsAssignableFrom(parentType) &&
                            !ReflectionHelper.GetMscorlibType(typeof(IDictionary)).IsAssignableFrom(parentType))
#else
                            !typeof(IEnumerable).IsAssignableFrom(parentType) &&
                            !typeof(IList).IsAssignableFrom(parentType) &&
                            !typeof(IDictionary).IsAssignableFrom(parentType))
#endif
                        {
                            ThrowException(SRID.ParserCannotAddAnyChildren, parentType.FullName);
                        }

                        break;
                    }
                case ElementContextType.PropertyArray:
                    {
                        // Make sure our type matches the type accepted by the array
                        Type arrayType = ParentContext.ContextDataType;
                        Type objectType = CurrentContext.ContextDataType;
                        if (!(objectType == KnownTypes.Types[(int)KnownElements.ArrayExtension])
                            && !arrayType.IsAssignableFrom(objectType))
                        {
                            ThrowException(SRID.ParserBadTypeInArrayProperty,
                                           arrayType.FullName,
                                           objectType.FullName);
                        }
                        break;
                    }
                case ElementContextType.PropertyComplex:
                case ElementContextType.PropertyIDictionary:
                case ElementContextType.PropertyIList:
                    // No restrictions.
                    break;

                default:
                    // Other context types can't have objects as children
                    ThrowException(SRID.ParserNoChildrenTag, ParentContext.ContextData.ToString());
                    break;
            }
        }

        #endregion // Elements

        #region Text

        // Called by CompileText() to make a decision on what to do with the
        //  text within element tags.
        private void CompileTextUnderElement(string textValue, bool isWhitespace)
        {
            Type elementType = CurrentContext.ContextDataType;
            if (BamlRecordManager.TreatAsIAddChild(elementType))
            {
                // This text will go into IAddChild::AddText(), which eliminates the
                //  possibility that we'll use a TypeConverter to create this element..

                TokenReaderNodeCollection.ResetTypeConverterDecision();

                WriteText(textValue, null, XmlReader.Depth); // Text for IAddChild::AddText()
            }
            else if(CurrentContext.IsContentPropertySet)
            {
                // The parent element has a ContentPropertyAttribute.  See if
                //  this content property can take text.
                bool contentText = false;
                Type contentPropertyType = XamlTypeMapper.GetPropertyType(CurrentContext.ContentPropertyInfo);

#if PBTCOMPILER
                if ( contentPropertyType == ReflectionHelper.GetMscorlibType(typeof(object)) ||
                    contentPropertyType == ReflectionHelper.GetMscorlibType(typeof(string)) )
#else
                if ( contentPropertyType == typeof(object) ||
                    contentPropertyType == typeof(string) )
#endif
                {
                    // This content property takes a string directly
                    contentText = true;
                }
#if PBTCOMPILER
                else if( ReflectionHelper.GetMscorlibType(typeof(IList)).IsAssignableFrom(contentPropertyType) )
#else
                else if( typeof(IList).IsAssignableFrom(contentPropertyType) )
#endif
                {
                    // This string will go into IList.Add(object)

                    // We need to see if this is a strongly-typed collection.  If so,
                    // it may not accept strings directly.

                    if( CanCollectionTypeAcceptStrings( contentPropertyType ))
                    {
                        contentText = true;
                    }

                }

                // (ContentProperty wants text) AND
                //      ( (Text isn't Whitespace) OR
                //        (ContentProperty wants whitespace text anyway) OR
                //        (ContentProperty is type string, which unconditionally gets whitespaces) )
                if( contentText &&
                    (!isWhitespace ||
                     IsWhitespaceSignificantAttributePresent(contentPropertyType) ||
#if PBTCOMPILER
                     contentPropertyType == ReflectionHelper.GetMscorlibType(typeof(string))))
#else
                     contentPropertyType == typeof(string)))
#endif
                {
                    if(CurrentContext.ContentParserState != ParsingContent.During)
                    {
                        // This text is the first content node.
                        CompileContentProperty(CurrentContext); // Write Content property information.
                    }

                    // Text is going into Content and will not be input to a TypeConverter.
                    TokenReaderNodeCollection.ResetTypeConverterDecision();

                    WriteText(textValue, null, XmlReader.Depth); // Text for object/string/IList ContentProperty
                }
                else if( !isWhitespace )
                {
                    // ContentProperty does not accept text, try to use
                    //  this as TypeConverter input at runtime.
                    Type converterType = XamlTypeMapper.GetTypeConverterType(elementType);
                    if (converterType == null)
                    {
                        ThrowException(SRID.ParserDefaultConverterElement, elementType.FullName, textValue);
                    }

                    TokenReaderNodeCollection.WritingTypeConverterText(textValue);
                    WriteText(textValue, converterType, XmlReader.Depth); // Text for TypeConverter
                }
                else
                {
                    // Text is whitespace that we will ignore.
                }
            }
            else if( !isWhitespace )
            {
                // The parent element does not have a ContentPropertyAttribute.
                //  Try to use this as TypeConverter input at runtime.
                Type converterType = XamlTypeMapper.GetTypeConverterType(elementType);
                if (converterType == null)
                {
                    ThrowException(SRID.ParserDefaultConverterElement, elementType.FullName, textValue);
                }

                TokenReaderNodeCollection.WritingTypeConverterText(textValue);
                WriteText(textValue, converterType, XmlReader.Depth); // Text for TypeConverter
            }
            else
            {
                // Text is whitespace that we will ignore.
            }
        }



        // Determine if a collection type can accept strings.  I.e. if the collection
        // is ICollection<T>, and T isn't a string or object, then it doesn't accept
        // string items.

        internal static bool CanCollectionTypeAcceptStrings( Type collectionType )
        {
            short collectionTypeID = BamlMapTable.GetKnownTypeIdFromType(collectionType);

            Debug.Assert( collectionTypeID >= 0
                              ||
                              KnownTypes.CanCollectionTypeAcceptStrings( (KnownElements) (-collectionTypeID) )
                                 == CanCollectionTypeAcceptStringsHelper( collectionType ));


            return collectionTypeID < 0
                   &&
                   KnownTypes.CanCollectionTypeAcceptStrings( (KnownElements) (-collectionTypeID) )
                   ||
                   CanCollectionTypeAcceptStringsHelper( collectionType );
        }


        // Implementation for CanCollectionPropertyAcceptStrings.
        // This is also called by KnownTypesInitializer
        internal static bool CanCollectionTypeAcceptStringsHelper( Type propertyType )
        {
            // We need to see if this is a strongly-typed collection.  If so,
            // it may not accept strings directly.

            // First, get the type of the collection.  The means for this in the
            // Xaml design is to look at ICollection<T>.  This call returns T.
            Type collectionItemType = GetCollectionItemType(propertyType);

            // Just like above, if the item type is object or string, then yes,
            // type type accepts text content.
#if PBTCOMPILER
            if( collectionItemType == ReflectionHelper.GetMscorlibType(typeof(object))
                ||
                collectionItemType == ReflectionHelper.GetMscorlibType(typeof(string)) )
#else
            if( collectionItemType == typeof(object)
                ||
                collectionItemType == typeof(string) )
#endif
            {
                return true;
            }

            // Otherwise, see if this type supports ContentPropertyWrapper, which wraps
            // text as a compatible type.
            else if( CanWrapStringAsItemType(propertyType, collectionItemType ))
            {
                return true;
            }

            return false;

        }


        //summary
        // Given a collection type (IList), this method determines the valid types
        // that can be added to the collection.  This is based on the pattern that even if
        // a type supports IList (which accepts Object), if it also implements ICollection<T>,
        // then it really only supports T types.
        ///summary

        internal static Type GetCollectionItemType( Type collectionType )
        {
            // Get the concrete ICollection<T> interface.  The defined name mangling pattern
            // for generics is a ` character followed by the number of arguments.
            Type iCollectionT = collectionType.GetInterface("System.Collections.Generic.ICollection`1");
            if( iCollectionT != null )
            {
                // Make sure this is really ICollection<T> in mscorlib, not an interface with the same
                // name/namespace.

#if PBTCOMPILER
                if( iCollectionT.Assembly == ReflectionHelper.GetMscorlibType(typeof(IList)).Assembly )
#else
                if( iCollectionT.Assembly == typeof(IList).Assembly )
#endif
                {
                    // Return the T
                    return iCollectionT.GetGenericArguments()[0];
                }
            }

            // If we get here, it's not a valid ICollection<T>, so it's an untyped collection,
            // thus accepts items of type Object.

#if PBTCOMPILER
            return ReflectionHelper.GetMscorlibType(typeof(Object));
#else
            return typeof(Object);
#endif

        }


        //summary
        // This method looks for the Xaml language pattern around ContentWrapper.  That pattern
        // is to treat ContentWrapper essentially as an implicit conversion operator.  E.g.
        // [ContentWrapper(Run)] on a collection indicates that the collection suggests Run as
        // a wrapper for another type, specified in its constructor.  So, since Run has a String
        // constructor, we discover here that a string can be converted into a Run.
        // This was created to solve the scenario of the InlineCollection, which is a typed
        // collection (ICollection<Inline>), but that also takes strings.  Ultimately,  it's to
        // enable the <TextBlock>Hello</TextBlock> scenario.
        ///summary

        internal static bool CanWrapStringAsItemType( Type collectionType, Type collectionItemType )
        {
            Type contentWrapper = null;  //e.g. Run

            // Get the attribute list

           #if PBTCOMPILER
            IList<CustomAttributeData> attributes = CustomAttributeData.GetCustomAttributes(collectionType);
           #else
            AttributeCollection attributes = TypeDescriptor.GetAttributes(collectionType);
           #endif

            for( int i = 0; i < attributes.Count; i++ )
            {
                // See if this is a ContentWrapperAttribute

               #if PBTCOMPILER
                // We can't use typeof(ContentWrapper) because it would be in the wrong assembly.
                // Use KnownTypes instead.
                if( attributes[i].Constructor.ReflectedType == KnownTypes.Types[(int)KnownElements.ContentWrapperAttribute] )
               #else
                ContentWrapperAttribute contentWrapperAttribute = attributes[i] as ContentWrapperAttribute;
                if( contentWrapperAttribute != null )
               #endif
                {
                    // Now look at the type specified in this ContentWrapperAttribute.
                    // E.g. contentWrapper might be typeof(Run)

                   #if PBTCOMPILER
                    contentWrapper = attributes[i].ConstructorArguments[0].Value as Type;
                   #else
                    contentWrapper = contentWrapperAttribute.ContentWrapper;
                   #endif

                    // Get the type's content property name
                    string contentPropertyName = GetContentPropertyName(contentWrapper);
                    if( contentPropertyName == null )
                    {
                        continue;
                    }

                    // And the content property itself
                    PropertyInfo propertyInfo = contentWrapper.GetProperty( contentPropertyName );
                    if( propertyInfo == null )
                    {
                        continue;
                    }

                    // And then see if the CPA accepts string.
#if PBTCOMPILER
                    if( propertyInfo.PropertyType.IsAssignableFrom(ReflectionHelper.GetMscorlibType(typeof(string))) )
#else
                    if( propertyInfo.PropertyType.IsAssignableFrom(typeof(string)) )
#endif
                    {
                        return true;
                    }


                }
            }


            return false;

        }


        // Called by CompileText() to make a decision on what to do with the
        //  text within a complex property.
        private void CompileTextUnderComplexProperty(string textValue, bool isWhitespace)
        {
            // This code path is not called if text is the only content under a
            //  complex property, because that was diverted upstream
            //  and treated as if it's not a complex property at all.

            // Example:

            //  <Rectangle>
            //   <Rectangle.Fill>
            //    Blue               ----->  <Rectangle Fill="Blue"/>
            //   </Rectangle.Fill>
            //  </Rectangle>

            // Therefore, non-whitespace text here means there is at least one
            //  non-text element as a sibling to this text node.  This is only
            //  allowed if the complex property here is a collection type
            //  that can handle multiple child elements.  (IList, etc.)

            if( IsACollection(ParentContext.ChildPropertyType) )
            {
                if( isWhitespace &&
                    !IsWhitespaceSignificantAttributePresent(ParentContext.ChildPropertyType))
                {
                    // If this is whitespace text AND the property isn't marked
                    //  as saying whitespace is significant, ignore the whitespaces.
                    return;
                }

                // (1) Non-whitespace text, or
                // (2) A collection-type complex property that wants to see all the whitespaces.
                WriteText(textValue, null, XmlReader.Depth); // Text for (Complex collection property)::Add()
            }
            else
            {
                // This complex property cannot take multiple elements.
                //  Whitespace is OK but anything else will cause an exception.
                if( isWhitespace )
                {
                    return;
                }
                else
                {
                    ThrowException(SRID.ParserTextInComplexProp,
                                   textValue,
                                   CurrentContext.ChildTagLocalName);
                }
            }
        }

        // Process text from the XML to see if it will be output as a XamlTextNode.

        // If this text string came from the content of a single XML text node,
        //  the xmlNodeType parameter will reflect the node returned by the XML
        //  reader.  There are four expected types in this class:
        //  (1) CDATA (2) Text (3) SignificantWhitespace (4) Whitespace

        // But when the string parameter is a result of combining information from
        //  multiple XML text nodes, we can't necessarily call it any of those
        //  four.  For that scenario this method expects XmlNodeType.None

        private void CompileText(XmlNodeType xmlNodeType, string textValue)
        {
            // Determine whether 'textValue' is whitespace.
            bool isWhitespace;
            Debug.Assert( xmlNodeType == XmlNodeType.Text ||
                          xmlNodeType == XmlNodeType.Whitespace ||
                          xmlNodeType == XmlNodeType.SignificantWhitespace ||
                          xmlNodeType == XmlNodeType.CDATA ||
                          xmlNodeType == XmlNodeType.None,
                          "Internal error - the caller method should not have passed in a XML node type of " + xmlNodeType);
            switch(xmlNodeType)
            {
                case XmlNodeType.Text:
                    isWhitespace = false;
                    break;

                case XmlNodeType.Whitespace:
                case XmlNodeType.SignificantWhitespace:
                    isWhitespace = true;
                    break;

                case XmlNodeType.CDATA:
                case XmlNodeType.None:

                default: // 'default' should never be hit, checked builds will verify with assert above.

                    // Walk the length of the string, looking for non-whitespace chars.
                    isWhitespace = IsWhitespace(textValue);
                    break;
            }

            // CurrentContext holds information about the element that surrounds this text.
            // The only time we don't have CurrentContext is if we're outside the
            //  root element.  During normal execution, we might see some whitespace
            //  either before or after the root element, it's fine to ignore these.
            if( CurrentContext == null )
            {
                Debug.Assert( isWhitespace,
                    "Non-whitespace text must be between element tags.  Text outside of the root tag should have been handled as errors upstream.  (Say, by XmlCompatibilityReader/XmlTextReader.)");

                return;
            }

            // What we do with the text depends on the context type of the parent node hosting this text.
            ElementContextType parentNodeType = CurrentContext.ContextType;

            if( parentNodeType == ElementContextType.Default )
            {
                // Text under object element tags
                CompileTextUnderElement(textValue, isWhitespace);
            }
            else if( parentNodeType == ElementContextType.PropertyComplex )
            {
                // Text under complex property tags
                CompileTextUnderComplexProperty(textValue, isWhitespace);
            }
            else if( parentNodeType == ElementContextType.PropertyIList )
            {
                // We'll give non-whitespace text to IList::Add(object).
                // If this IList type wants whitespaces, it'll get those too.
                if( !isWhitespace || IsWhitespaceSignificantAttributePresent(ParentContext.ChildPropertyType) )
                {
                    WriteText(textValue, null, XmlReader.Depth); // Text for property IList::Add()
                }
            }
            else if( parentNodeType == ElementContextType.PropertyArray ||
                     parentNodeType == ElementContextType.PropertyIDictionary )
            {
                // Text under Array/IDictionary tags
                if( isWhitespace )
                {
                    // Whitespace under Array/Dictionary properties are ignored.
                    return;
                }

                // Non-whitespace text is forbidden.
                ThrowException(SRID.ParserTextInvalidInArrayOrDictionary,
                               CurrentContext.ContextData == null ? "?" : CurrentContext.ContextData.ToString(),
                               string.Empty);
            }
            else
            {
                // Assert if it's not ElementContextType.Unknown.  If this is not
                //  a bug in the calling code, we'll need a new clause in the
                //  if/else tree above to properly handle the new context type.
                Debug.Assert (parentNodeType == ElementContextType.Unknown,
                    "This method does not expect to see element context type of " + parentNodeType);

                // Sometimes we just don't know what the element is.  This occurs,
                //  for example, during pass 1 of compilation when the object being
                //  referred to doesn't exist yet.
                // Given that we have no information to make intelligent decisions,
                //  the raw text will be stored as a XamlTextNode.
                WriteText( textValue, null, XmlReader.Depth ); // Text saved for Unknown purposes.
            }
        }

        bool IsAttributePresentationOptionsFreeze(string attributeLocalName, string attributeNamespaceUri)
        {
            // Is this the 'Freeze' attribute in the PresentationOptions namespace?
            return (attributeLocalName == PresentationOptionsFreeze) &&
                   (attributeNamespaceUri == PresentationOptionsNamespaceURI);
        }


        #endregion Text

        #region ProcessingInstructions

        // States for parsing state maching for Mapping Processing Instructions
        private enum PIState
        {
            NotFound = 0,
            MatchingKey = 1,
            MatchedKey = 2,
            EqualsFound = 3,
            BuildingValue = 4,
            ValueFound = 5,
        }

        private bool CompilePI()
        {
            //we want to ignore all PIs
            return false;
        }

        #endregion ProcessingInstructions

        #endregion // BamlContext

        #region ParserWrappers

        // Methods and properties that wrap the XmlReader and keep track of parser state



        /// <summary>
        /// States of the Parser loop
        /// </summary>
        enum ParserState
        {
            Uninitialized,   // initial state.
            ReadingFirstTag, // reading the first tag.
            Reading,         // reading the rest of the document
            Done,            // end reading the document.
        }


        // state properties for the main Parser Loop so it can be started and stopped.
        ParserState ParseLoopState
        {
            get { return _parseLoopState; }
            set { _parseLoopState = value; }
        }



        /// <summary>
        ///  Determines if XmlReader has parsed all the data.
        ///  need this because Validating reader never set the EOF if the only
        ///  node it passed in text.
        /// </summary>
        /// <returns>True if more data</returns>
        bool IsMoreData()
        {
            bool result = true;

            // if EOF or we aren't in the initialized state but or depth is 0 meaning we've done
            // a read and are depth is zero which can only happen at the end of file.
            if ((XmlReader.EOF) || ((ParseLoopState != ParserState.Uninitialized) &&
                    0 == XmlReader.Depth && XmlReader.NodeType == XmlNodeType.None))
            {
                result = false;
            }

            return result;

        }

        // Property to return the current instance of the XmlReader
        internal XmlReader XmlReader
        {
            get { return _xmlReader; }
            set
            {

                _xmlReader = value;

                // setup the LineInfo
                if (null != _xmlReader)
                {
                    _xmlLineInfo = _xmlReader as IXmlLineInfo;
                    // Xml Reader must be able to return line and position for error reporting
                    // and for tracking the exact position of the reader in Xml.
                    if (_xmlLineInfo == null)
                    {
                        ThrowException(SRID.ParserXmlReaderNoLineInfo,
                                       _xmlReader.GetType().FullName);
                    }
                }
                else
                {
                    _xmlLineInfo = null;
                }
            }
        }


        /// <summary>
        /// sets normilization
        /// </summary>
        bool Normalization
        {
            set
            {

                Debug.Assert(null != XmlReader, "XmlReader is not yet set");
                //check if it's a XmlCompatibilityReader first
                XmlCompatibilityReader xmlCompatReader = XmlReader as XmlCompatibilityReader;
                if (null != xmlCompatReader)
                {
                    xmlCompatReader.Normalization = true;
                }
                else
                {
                    //now check for XmlTextReader
                    XmlTextReader xmlTextReader = XmlReader as XmlTextReader;

                    // review, what if not the XmlTextReader.
                    if (null != xmlTextReader)
                    {
                        xmlTextReader.Normalization = true;
                    }
                }
            }
        }


        /// <summary>
        /// Property for LineNumber.  This is the current reader's line number plus
        /// any offset specified in the parser context.  The parser context holds the
        /// starting line position when parsing a section of markup in a larger file.
        /// </summary>
        internal int LineNumber
        {
            get
            {
                // If we are starting parsing at some location other than the start of
                // a file, then the parser context line number indicates where the first
                // line for _xmlLineInfo is, so subtract one to get the overall file
                // line number.
                if (null != _xmlLineInfo)
                {
                    if (_parserContext.LineNumber > 0)
                    {
                        return _xmlLineInfo.LineNumber + _parserContext.LineNumber - 1;
                    }
                    else
                    {
                        return _xmlLineInfo.LineNumber;
                    }
                }
                else
                {
                    Debug.Assert(false, "XmlReader doesn't support LineNumber");
                    return 0;
                }
            }
        }

        /// <summary>
        /// Property for LinePosition.  This is the current reader's line position plus
        /// any offset specified in the parser context.  The parser context holds the
        /// starting line position when parsing a section of markup in a larger file.
        /// </summary>
        internal int LinePosition
        {
            get
            {
                if (null != _xmlLineInfo)
                {
                    // If we are starting parsing at some location other than the start of
                    // a file, then we have to add the offset from the ParserContext
                    if (_parserContext.LineNumber > 0 &&
                        _xmlLineInfo.LineNumber == 1)
                    {
                        return _xmlLineInfo.LinePosition + _parserContext.LinePosition;
                    }
                    else
                    {
                        return _xmlLineInfo.LinePosition;
                    }
                }
                else
                {
                    Debug.Assert(false, "XmlReader doesn't support LinePosition");
                    return 0;
                }
            }
        }


        #endregion ParserWrappers

        #region TextHandling

        // region for Texthandling helpers for whitespace handling.


        /// <summary>
        /// class fo holding TextFlow data that
        /// we place on the TextFlowStack
        /// </summary>
        internal class TextFlowStackData
        {
            // constructor to start a new text flow for the node.
            internal TextFlowStackData()
            {
                TextNode = null;
                _stripLeadingSpaces = true;
                _inlineCount = 0;
                _EncounteredIgnorableTag = false;
            }

            // property for the Current TextNode if any
            // for the flow that hasn't been processed.
            internal XamlTextNode TextNode
            {
                get { return _textNode; }
                set { _textNode = value; }
            }

            // set to true if all leading spaces should be
            // stripped from the TextNode.
            internal bool StripLeadingSpaces
            {
                get { return _stripLeadingSpaces; }
                set { _stripLeadingSpaces = value; }
            }

            // set to true of within an xml:space="preserve" scope
            // so know not to collapse text.
            // overrides the StripLeading spaces sproperty
            internal bool XmlSpaceIsPreserve
            {
                get { return _xmlSpaceIsPreserve; }
                set { _xmlSpaceIsPreserve = value; }
            }

            /// <summary>
            /// number of inline tags deep the flow is such as a bold tag
            /// </summary>
            internal int InlineCount
            {
                get { return _inlineCount; }
                set { _inlineCount = value; }
            }

            /// <summary>
            /// Set to indicate an ignorable element like a comment was encountered while
            /// processing a section of text content.
            /// </summary>
            internal bool EncounteredIgnorableTag
            {
                get { return _EncounteredIgnorableTag; }
                set { _EncounteredIgnorableTag = value; }
            }

            // valid to be null
            // we keep the textNode until we know how to handle the end of
            // line whitespace.
            XamlTextNode _textNode;

            // true if should strip the leading space from the next textRun.
            // Value is initially set to True.
            // If this value is True all of the leading Whitepace is removed from
            // the current Node. else the leading whitespace is collapsed into a single whitespace.
            // review if should cal StripAllLeadingSpace.
            bool _stripLeadingSpaces;

            bool _xmlSpaceIsPreserve;

            // count of how many inline tags we are deep increment/decrement for each inline tag
            // on a ProcessBeginTag/ProcessEndTag of type Inline.
            int _inlineCount;

            // Set to indicate an ignorable element like a comment was encountered while
            // processing a section of text content.
            bool _EncounteredIgnorableTag;
        }

        // returns true if the current character is a whiteSpace character
        // \t\r\n\f
        // resuse CSSChar definitions
        static internal bool IsWhiteSpace(char c)
        {
            if (c == CSSChar.Tab || c == CSSChar.Return
                || c == CSSChar.NewLine || c == CSSChar.FormFeed
                || c == CSSChar.Space)
            {
                return true;
            }

            return false;
        }

        // Skips All whitespace from the StartIndex and returns
        // the character offset of the first non-whitespace char.
        // or endIndex if no whitespace.
        // caller should pass in an endIndex which is one greater
        // than the characters they want to check.
        int SkipWhitespace(string text, int startIndex, int endIndex)
        {
            int index = startIndex;

            while (index < endIndex)
            {
                if (!IsWhiteSpace(text[index]))
                {
                    break;
                }

                ++index;
            }

            return index;

        }

        // Return true if the passed string is only whitespace
        static internal bool IsWhitespace(string text)
        {
            for (int i = 0; i < text.Length; i++)
            {
                if (!IsWhiteSpace(text[i]))
                {
                    return false;
                }
            }

            return true;
        }

        // given a string and the current index into it, calculates the unicode scalar value of
        // the char previous (isPrevious=true) or next (isPrevious=false) to it.
        static int GetAdjacentUnicodeScalarValue(string s, int currentPos, bool isPrevious)
        {
            Debug.Assert(currentPos > 0 && currentPos < s.Length - 1);
            int unicodeScalarValue = 0;
            int index = currentPos + (isPrevious ? -2 : 1);
            if (index < 0)
            {
                // there is only one char before the current, so it can't be a surrogate.
                unicodeScalarValue = s[0];
            }
            else
            {
                bool isSurrogate = false;
                char highChar = s[index];
                if (char.IsHighSurrogate(highChar))
                {
                    char lowChar = s[index + 1];
                    // Can we assert for this?
                    if (char.IsLowSurrogate(lowChar))
                    {
                        // both high & low surrogate exist, so get the scalar value by combining
                        // the lower 10 bits from each.
                        unicodeScalarValue = (((highChar & 0x03FF) << 10) | (lowChar & 0x3FF)) + 0x10000;
                        isSurrogate = true;
                    }
                }

                if (!isSurrogate)
                {
                    // if not a surrogate, just return the previous or next char
                    unicodeScalarValue = isPrevious ? s[index + 1] : highChar;
                }
            }

            return unicodeScalarValue;
        }

        // given a unicode scalar value, determines if it falls in the range
        // of an east asian code point
        static bool IsEastAsianCodePoint(int unicodeScalarValue)
        {
            if ((unicodeScalarValue >= 0xFF00 && unicodeScalarValue <= 0xFFEF)   || // Halfwidth and Fullwidth forms
                (unicodeScalarValue >= 0xF900 && unicodeScalarValue <= 0xFAFF)   || // CJK Compatibility
                (unicodeScalarValue >= 0xAC00 && unicodeScalarValue <= 0xD7A3)   || // Hangul Syllables
                (unicodeScalarValue >= 0xA000 && unicodeScalarValue <= 0xA4CF)   || // Yi
                (unicodeScalarValue >= 0x4E00 && unicodeScalarValue <= 0x9FFF)   || // CJK Unified Ideographs
                (unicodeScalarValue >= 0x3400 && unicodeScalarValue <= 0x4DFF)   || // CJK Unified Ideographs Extension A
                (unicodeScalarValue >= 0x31F0 && unicodeScalarValue <= 0x31FF)   || // Katakana Phonetic Extensions
                (unicodeScalarValue >= 0x3190 && unicodeScalarValue <= 0x319F)   || // Kanbun
                (unicodeScalarValue >= 0x3130 && unicodeScalarValue <= 0x318F)   || // Hangul Compatibility Jamo
                (unicodeScalarValue >= 0x3100 && unicodeScalarValue <= 0x312F)   || // Bopomofo
                (unicodeScalarValue >= 0x30A0 && unicodeScalarValue <= 0x30FF)   || // Katakana
                (unicodeScalarValue >= 0x3040 && unicodeScalarValue <= 0x309F)   || // Hiragana
                (unicodeScalarValue >= 0x2FF0 && unicodeScalarValue <= 0x2FFB)   || // Ideographic Description
                (unicodeScalarValue >= 0x2E80 && unicodeScalarValue <= 0x2FD5)   || // CJK and KangXi Radicals
                (unicodeScalarValue >= 0x1100 && unicodeScalarValue <= 0x11FF)   || // Hangul
                // surrogates
                (unicodeScalarValue >= 0x20000 && unicodeScalarValue <= 0x2a6d6) || // CJK Unified Ext. B
                (unicodeScalarValue >= 0x2F800 && unicodeScalarValue <= 0x2FA1D))   // CJK Compatibility Supplement
            {
                return true;
            }

            return false;
        }

        // collapses text, review if can use existing stringBuilder.
        string CollapseText(
                string text,
                bool stripAllLeadingSpaces,
                bool stripAllRightWhitespace,
                bool preserve,
            out bool endedWithWhiteSpace)
        {
            int textLength = text.Length;
            int textIndex = 0;

            Debug.Assert(null != text, "String passed to CollapseText cannot be null");
            Debug.Assert(textLength > 0, "String passed to Collapsed Text cannot be empty");

            endedWithWhiteSpace = false;
            string collapsedText;

            if (!preserve)
            {
                StringBuilder collapsedTextStringBuilder = new StringBuilder(text.Length);

                // now loop until hit the end.
                while (textIndex < textLength)
                {
                    char currentChar = text[textIndex];
                    // if not whitespace just copy
                    if (!IsWhiteSpace(currentChar))
                    {
                        collapsedTextStringBuilder.Append(currentChar);
                        ++textIndex;
                    }
                    else
                    {
                        // if whitespace see where the whitepsace ends
                        int endWhitespaceIndex = SkipWhitespace(text, textIndex, textLength);

                        // if the entire run is whitespace don't add if either
                        // stripAllRightWhitespace || stripAllLeadingSpaces is true

                        if (0 == textIndex && endWhitespaceIndex >= textLength)
                        {
                            if (!stripAllRightWhitespace && !stripAllLeadingSpaces)
                            {
                                collapsedTextStringBuilder.Append(CSSChar.Space);
                            }

                            endedWithWhiteSpace = true;

                        }
                        else if (0 == textIndex)
                        {
                            // if  text run began with a whitespace then collapse
                            // based on stripAllLeadingSpaces

                            if (!stripAllLeadingSpaces)
                            {
                                collapsedTextStringBuilder.Append(CSSChar.Space);
                            }
                        }
                        else if (endWhitespaceIndex >= textLength)
                        {
                            // textRun ends with whitespace
                            if (!stripAllRightWhitespace)
                            {
                                collapsedTextStringBuilder.Append(CSSChar.Space);
                            }

                            endedWithWhiteSpace = true;
                        }
                        else
                        {
                            // we are somewhere in the middle.
                            bool collapseNewLine = false;

                            // if current char is a new line, strip it out if it is immediately surrounded
                            // by East Asian chars on both sides.
                            if (currentChar == CSSChar.NewLine)
                            {
                                // as an optimization, do the checks only if prev char could possibly
                                // be beyond the start of the lowest East Asian CodePoint range.
                                char prevChar = text[textIndex - 1];
                                if (prevChar >= 0x1100)
                                {
                                    // get the unicode scalar value for the char before the current textIndex
                                    int unicodeScalarValue = GetAdjacentUnicodeScalarValue(text, textIndex, true);
                                    // check if it is an east asian char
                                    collapseNewLine = IsEastAsianCodePoint(unicodeScalarValue);
                                    // if it is ...
                                    if (collapseNewLine)
                                    {
                                        // ... get the unicode scalar value for the char after the current textIndex
                                        unicodeScalarValue = GetAdjacentUnicodeScalarValue(text, textIndex, false);
                                        // check if it is an east asian char
                                        collapseNewLine = IsEastAsianCodePoint(unicodeScalarValue);
                                    }
                                }
                            }

                            // collapse to space if not surrounded by east asian chars on either side.
                            if (!collapseNewLine)
                            {
                                collapsedTextStringBuilder.Append(CSSChar.Space);
                            }
                        }

                        textIndex = endWhitespaceIndex;
                    }
                }

                collapsedText = collapsedTextStringBuilder.ToString();
            }
            else
            {
                // for preserve we still need to indicate if the lastCharacter was a space in
                // case the next text in the run is not preserved.

                if (textLength > 0)
                {
                    if (IsWhiteSpace(text[textLength - 1]))
                    {
                        endedWithWhiteSpace = true;
                    }
                    else
                    {
                        endedWithWhiteSpace = false;
                    }
                }
                collapsedText = text;

            }
            return collapsedText;
        }

        private bool IsWhitespaceSignificantAttributePresent(Type collectionType)
        {
            bool returnValue=false;
            ElementContextStackData context = CurrentContext;

            if(context.IsWhitespaceSignificantCollectionAttributeKnown)
                return context.IsWhitespaceSignificantCollectionAttributePresent;

#if !PBTCOMPILER
            object[] attrs = collectionType.GetCustomAttributes(typeof(WhitespaceSignificantCollectionAttribute), true);
            if (attrs.Length == 1)
                returnValue = true;
#else
            Type baseType = collectionType;
            Type attrType = KnownTypes.Types[(int)KnownElements.WhitespaceSignificantCollectionAttribute];

            // Keep looking up the base class hierarchy for this attribute
            while (null != baseType)
            {
                IList<CustomAttributeData> list = CustomAttributeData.GetCustomAttributes(baseType);
                foreach (CustomAttributeData cad in list)
                {
                    if (cad.Constructor.ReflectedType == attrType)
                         returnValue = true;
                }
                baseType = baseType.BaseType;
            }
#endif

            context.IsWhitespaceSignificantCollectionAttributePresent = returnValue;
            return returnValue;
        }

        #endregion TextHandling

        #region IDictionaryData

        // Class to cache information relating to IDictionaries.  This is used
        // for duplicate x:Key processing
        private class DictionaryContextData
        {
            internal DictionaryContextData(
                Type propertyType)
            {
                _propertyType = propertyType;
            }

            internal Type PropertyType
            {
                get { return _propertyType; }
            }

            internal bool ContainsKey(object key)
            {
                if (_keyDictionary == null)
                {
                    return false;
                }
                else
                {
                    return _keyDictionary.Contains(key);
                }
            }

            internal void AddKey(object key)
            {
                if (_keyDictionary == null)
                {
                    _keyDictionary = new HybridDictionary();
                }
                _keyDictionary[key] = null;
            }


            private Type _propertyType;
            private HybridDictionary _keyDictionary;

        }


        #endregion IDictionaryData

        #region XamlNodeCollection

        /// <summary>
        /// A collection of just-generated XamlNode objects that have not yet
        /// been processed by XamlParser, plus additional capabilities to extract
        /// information from the collection on hand.
        /// </summary>

        // At heart this class is an ArrayList that holds a bunch of XamlNodes
        //  that have just been generated from XML nodes.  But this class does more
        //  than just wrap the ArrayList.

        // The primary function is a FIFO queue: Add() to the end, Remove() from
        //  the front.

        // Secondary manipulation functions: The two Insert operations allow
        //  special-case functionality when we want to do things out of FIFO order.
        //  The IsMarkedForInsertion state determines when these insert operations are legal.

        // Information extraction capabilities: This class is also responsible
        //  for extracting higher-level information out of a set of nodes that
        //  cannot be reliably inferred by looking at individual nodes.  The original
        //  motivation for this level of functionality is to determine whether
        //  a TypeConverter is applicable for a particular element.

        internal class XamlNodeCollectionProcessor
        {
            /// <summary>
            /// Contructor
            /// </summary>
            internal XamlNodeCollectionProcessor()
            {
            }

            //////////////////////////////////////////////////////////////////////////
            //
            //  First-In First-Out queue operations

            /// <summary>
            /// Enqueues the given node to the end of the ArrayList buffer.
            /// </summary>
            internal void Add(XamlNode xamlNode)
            {
                _xamlNodes.Add(xamlNode);

                ExamineAddedNode(xamlNode);
            }

            /// <summary>
            /// Dequeues a node from the beginning of the collection, and advance the
            ///  "next node" pointer in preparation for the next Remove() call.
            /// </summary>
            /// <returns></returns>
            internal XamlNode Remove()
            {
                XamlNode xamlNode = null;

                Debug.Assert((_nodeIndex == 0 && _xamlNodes.Count == 0 ) || (_nodeIndex < _xamlNodes.Count),
                    "Our 'next element' index is outside the range of available elements.  Since we assert this at the start and end of this function, either somebody modified the array of elements under us, or we're hitting some cross-thread issue.");
                if (_xamlNodes.Count > 0)
                {
                    xamlNode = (XamlNode)_xamlNodes[_nodeIndex];
                    ++_nodeIndex;

                    if (_nodeIndex == _xamlNodes.Count)
                    {
                        // We are returning the final available element in the
                        //  ArrayList.  This means we're done with one set of XamlNodes
                        //  and will soon get a different set.
                        // In preparation for the new set, we release the old ArrayList
                        //  and create a new one.  This is supposedly faster than
                        //  clearing and re-using the old ArrayList due to how the
                        //  garbage collector works.
                        _nodeIndex = 0;
                        _xamlNodes = new ArrayList(10);
                    }
                }
                Debug.Assert((_nodeIndex == 0 && _xamlNodes.Count == 0 ) || (_nodeIndex < _xamlNodes.Count),
                    "Our 'next element' index is now pointing outside the range of available elements.  This is not a valid state - see why the array or the pointer was modified incorrectly above.");

                return xamlNode;
            }

            /// <summary>
            /// Count of the number of nodes in the Collection
            /// </summary>
            internal int Count
            {
                get { return _xamlNodes.Count; }
            }

            //////////////////////////////////////////////////////////////////////////
            //
            //  Out-of-order insert operations

            // Marks the current index at which insertion of nodes can happen.
            internal bool IsMarkedForInsertion
            {
                get
                {
                    return _insertionIndex != -1;
                }
                set
                {
                    Debug.Assert(!value || _insertionIndex == -1, "Attribute node collection is already marked for insertion.");
                    _insertionIndex = value ? _xamlNodes.Count : -1;
                    _currentInsertionIndex = _insertionIndex;
                }
            }

            // Inserts node at the starting index when IsMarkedForInsertion is set to true.
            // This is currently used only for the Name property attribute node
            internal void InsertAtStartMark(XamlNode xamlNode)
            {
#if DBG
                Debug.Assert(IsMarkedForInsertion, "Attribute node collection is not marked for insertion.");
#endif
                _xamlNodes.Insert(_insertionIndex, xamlNode);
                ExamineInsertedNode(xamlNode, _insertionIndex);

                _currentInsertionIndex++;
            }

            // Inserts node at current index that is updated with each insertion.
            // This is currently used only for event attribute nodes.
            internal void InsertAtCurrentMark(XamlNode xamlNode)
            {
#if DBG
                Debug.Assert(IsMarkedForInsertion, "Attribute node collection is not marked for insertion.");
#endif
                _xamlNodes.Insert(_currentInsertionIndex, xamlNode);
                ExamineInsertedNode(xamlNode, _currentInsertionIndex );

                _currentInsertionIndex++;
            }

            //////////////////////////////////////////////////////////////////////////
            //
            //  Out-of-order remove operations

            // Remove the last ElementStart or EndAttributes rrecord in the token reader stack.
            // This may not be the very last item, since MappingPIs and other non-scoped nodes
            // may be added at any point, so search backwards for the first ElementStart or
            // EndAttributes node.
            internal void RemoveLastElement()
            {
                if (_xamlNodes.Count > 0)
                {
                    for (int i = _xamlNodes.Count - 1; i >= _nodeIndex; i--)
                    {
                        if (((XamlNode)_xamlNodes[i]).TokenType == XamlNodeType.ElementStart ||
                             ((XamlNode)_xamlNodes[i]).TokenType == XamlNodeType.EndAttributes)
                        {
                            _xamlNodes.RemoveAt(i);
                            break;
                        }
                    }

                    // if we have read all the nodes in the array then clear it for the next call
                    if (_nodeIndex >= _xamlNodes.Count)
                    {
                        // if at the count then we've read all the records.
                        // reset the node information.
                        Debug.Assert(_nodeIndex == _xamlNodes.Count, "NodeIndex is larger than node count.");
                        _nodeIndex = 0;
                        _xamlNodes = new ArrayList(10);
                    }
                }
            }

            //////////////////////////////////////////////////////////////////////////
            //
            //  Determine TypeConverter suitability

            // The routines in this section implements a state machine that determines
            //  if a set of XamlNodes is in the correct form for TypeConverter syntax.
            // Aside from the correct form, there are certain other requirements for
            //  TypeConverter syntax, and they are checked in the CompileText() routine.
            //  If a violation is found to deviate from valid TypeConverter usage, CompileText()
            //  calls ResetTypeConverterDecision() here to reset the state machine.

            // Here are the state machine stages, and their descriptions:
            private enum TypeConverterDecisionState
            {
                Uninitialized,
                // The initial (reset) state, where we're looking for the element start tag.

                ElementStart,
                // We've seen the element start tag, TypeConverterCandidate now points to this tag.
                //  If we see properties on the element tag, reset.  (<Element Prop="Foo">)
                //  If we see another object element start tag, stay on ElementStart but set TypeConverterCandidate to new object tag. (<Element><Element2>)
                //  If we see a x:Key element start tag, go to IgnoringKeyElements. (<Element x:Key="{MarkupExtension}">)
                //  If we see any other start tag (property element), reset.  (<Element><Element.Prop>)

                IgnoringKeyElements,
                // IgnoringKeyElements : This is a loop to ignore all the element/property/etc of a
                //  MarkupExtension inside a x:Key tag.  We ignore everything until we see the end marker to the
                //  x:Key information, at which time we resume the ElementStart state looking for text.

                InitializationString,
                // InitializationString: We've seen the string we want to give to the TypeConverter for TypeConverterCandidate.
                //  CompileText() has decided that the string is not going into object content.
                //  If we see more strings, merge the strings.
                //  If we see an object element tag, throw error. (<Element>TypeConverterText<Element2>)
                //  If we see a property element tag, throw error. (<Element>TypeConverterText<Element.Foo>)

                ElementEnd
                // ElementEnd: We've seen the end tag for the TypeConverterCandidate, and
                //  we plan on using the TypeConverter for the preceeding InitializationString text.
            }

            // Queried when we're deciding whether to read additional nodes from the
            //  XML reader.  Returns true if we need more nodes to decide.
            // Returns false if we've seen enough to decide, one way or another.
            internal bool IsTypeConverterUsageUndecided
            {
                get
                {
                    if( _typeConverterDecisionState == TypeConverterDecisionState.Uninitialized )
                    {
                        // We're not using the type converter
                        return false;
                    }
                    else if( _typeConverterDecisionState == TypeConverterDecisionState.ElementStart ||
                             _typeConverterDecisionState == TypeConverterDecisionState.IgnoringKeyElements ||
                             _typeConverterDecisionState == TypeConverterDecisionState.InitializationString )
                    {
                        // We may or may not want to use a type converter - can't tell yet.
                        return true;
                    }

                    Debug.Assert( _typeConverterDecisionState == TypeConverterDecisionState.ElementEnd,
                        "State machine checking for TypeConverter syntax has entered an invalid state.");

                    return false;
                }
            }

            // Called when we know a type converter will not be used.  This is called from
            //  inside this class if any part of the syntax fails.  It may be called from
            //  outside this class if caller determines that a TypeConverter is not applicable.
            // For example, when text is deemed to be content and not TypeConverter input.
            internal void ResetTypeConverterDecision()
            {
                if(_typeConverterTextWrittenAndNotProcessed != null)
                {
                    // This means we were expecting to use TypeConverter, we had
                    //  written out a text XamlNode expecting to use it, but we
                    //  came across something else that broke our ability to use
                    //  TypeConverter.

                    // Example that would trip this error:
                    //  <FontFamily>Symbol<FontFamily.Baseline>12.345</FontFamily.Baseline></FontFamily>
                    throw new InvalidOperationException(SR.Get(SRID.ParserAbandonedTypeConverterText,_typeConverterTextWrittenAndNotProcessed));
                }
                _typeConverterDecisionState = TypeConverterDecisionState.Uninitialized;
                _typeConverterCandidateIndex = 0;
                _typeConverterTextWrittenAndNotProcessed = null;
            }

            // Called by CompileText (or helper methods) when they are sending text
            //  that they think is TypeConverter input.  For proper TypeConverter
            //  syntax, we must be at a point where we're processing ElementStart.
            internal void WritingTypeConverterText(string initializationText)
            {
                if( _typeConverterDecisionState != TypeConverterDecisionState.ElementStart )
                {
                    // Example that would trip this error:
                    //  <FontFamily Baseline="12.345">Symbol</FontFamily>
                    throw new InvalidOperationException(SR.Get(SRID.ParserTypeConverterTextUnusable,initializationText));
                }

                _typeConverterTextWrittenAndNotProcessed = initializationText;
            }

            // A new node has been inserted into the middle of the XamlNode queue.
            //  See if this invalidates TypeConverter syntax.
            private void ExamineInsertedNode(XamlNode xamlNode, int insertionIndex)
            {
                if( _typeConverterDecisionState != TypeConverterDecisionState.Uninitialized )
                {
                    // This method only cares about the new node if we're in the middle of
                    //  a potential TypeConverter sequence.
                    // This is a shortcut enabled by the current design, where only a
                    //  limited set of XamlNodes can be legally inserted out-of-order.
                    if( NodeTypePrecludesTypeConverterUse(xamlNode) )
                    {
                        // The newly inserted node breaks our ability to use TypeConverter
                        ResetTypeConverterDecision();
                    }
                    else if( _typeConverterCandidateIndex >= insertionIndex )
                    {
                        // Update the pointer to the candidate ElementStart.
                        _typeConverterCandidateIndex++;
                        Debug.Assert(((XamlNode)_xamlNodes[_typeConverterCandidateIndex]).TokenType==XamlNodeType.ElementStart,
                            "We've lost track of the ElementStart XamlNode after an XamlNode insertion.");
                    }
                }
                return;
            }

            // A new node has been added to the end of the XamlNode queue.
            //  See if this invalidates TypeConverter syntax.
            private void ExamineAddedNode(XamlNode xamlNode)
            {
                if( _typeConverterDecisionState == TypeConverterDecisionState.Uninitialized )
                {
                    // Starting a new element.
                    if( xamlNode.TokenType == XamlNodeType.ElementStart )
                    {
                        _typeConverterDecisionState = TypeConverterDecisionState.ElementStart;
                        _typeConverterCandidateIndex = _xamlNodes.Count - 1;

                        Debug.Assert(((XamlNode)_xamlNodes[_typeConverterCandidateIndex])==xamlNode,
                            "The node at the end of the queue is supposed to be the node we're examining.  Determine why the two don't match.");
                    }
                }
                else if( _typeConverterDecisionState == TypeConverterDecisionState.ElementStart )
                {
                    // Starting another new element.
                    if( xamlNode.TokenType == XamlNodeType.ElementStart )
                    {
                        // We've got an ElementStart inside another ElementStart
                        // The previous ElementStart could not be created via TypeConverter because
                        //  it needs to include this new Element.  But the new element might be a
                        //  candidate for TypeConverter creation.
                        // To handle this case, we stay in the ElementStart mode, but move the
                        //  candidate pointer to the new ElementStart.

                        // <Element>      <-- We started with this guy as TypeConverter candidate
                        //  <Element2>    <-- This is our current node, and the new candidate
                        //   TypeConverterText
                        //  </Element2>
                        // </Element>

                        _typeConverterCandidateIndex = _xamlNodes.Count - 1;

                        Debug.Assert(((XamlNode)_xamlNodes[_typeConverterCandidateIndex])==xamlNode,
                            "Supposedly we've just seen a new ElementStart, but it's not actually at the end of the XamlNode queue.  Determine why the two are out of sync.");
                    }
                    // This is the beginning of a large chunk of XamlNodes generated from
                    //  MarkupExtension information inside a x:Key value.  We ignore everything
                    //  inside this block and will return to the ElementStart state when this block ends.
                    // This logic needs to change if we start allowing nested KeyElements.
                    else if( xamlNode.TokenType == XamlNodeType.KeyElementStart )
                    {
                        _typeConverterDecisionState = TypeConverterDecisionState.IgnoringKeyElements;
                    }
                    // Text for new element's TypeConverter
                    else if( xamlNode.TokenType == XamlNodeType.Text )
                    {
                        _typeConverterDecisionState = TypeConverterDecisionState.InitializationString;

                        Debug.Assert( _typeConverterTextWrittenAndNotProcessed != null,
                            "The caller had sent out TypeConverter initialization text - we should be seeing it now.");
                    }
                    else if( NodeTypePrecludesTypeConverterUse(xamlNode) )
                    {
                        // Reset state since this node isn't permitted in TypeConverter usage.
                        ResetTypeConverterDecision();
                    }
                    else
                    {
                        // Everything else are tolerated for TypeConverter use.
                    }
                }
                else if( _typeConverterDecisionState == TypeConverterDecisionState.IgnoringKeyElements )
                {
                    // We saw KeyElementStart, keep going until we see KeyElementEnd.
                    // This logic needs to change if we start allowing nested KeyElements.
                    if( xamlNode.TokenType == XamlNodeType.KeyElementEnd )
                    {
                        // Now we can resume examining things on the Element start tag.
                        _typeConverterDecisionState = TypeConverterDecisionState.ElementStart;
                    }
                }
                else if( _typeConverterDecisionState == TypeConverterDecisionState.InitializationString )
                {
                    // We've just seen the InitializationString, we expect it to be immediately
                    //  followed by an ElementEnd.
                    if( xamlNode.TokenType == XamlNodeType.ElementEnd )
                    {
                        Debug.Assert(((XamlNode)_xamlNodes[_typeConverterCandidateIndex]).TokenType==XamlNodeType.ElementStart,
                            "We've lost track of the ElementStart node, and we're about to die with a cast exception.  See if the ElementStart is still in the ArrayList somewhere, and find out why the pointer got out of sync.");

                        // We've seen the full <ElementStart>InitializationText</ElementEnd> sequence.
                        ((XamlElementStartNode)_xamlNodes[_typeConverterCandidateIndex]).CreateUsingTypeConverter = true;

                        // The initializationString would be used as input to the candidate element's TypeConverter.
                        _typeConverterTextWrittenAndNotProcessed = null;
                    }
                    else
                    {
                        // Example that would trip this error:
                        //  <FontFamily>Symbol<Button/></FontFamily>
                        throw new InvalidOperationException(SR.Get(SRID.ParserTypeConverterTextNeedsEndElement, _typeConverterTextWrittenAndNotProcessed));
                    }

                    // One set of XamlNodes for TypeConverter done, start watching for another.
                    ResetTypeConverterDecision();
                }
                return;
            }

            // Checking the given XamlNode against the list of types that we know will
            //  break our ability to use TypeConverter.
            private bool NodeTypePrecludesTypeConverterUse(XamlNode xamlNode)
            {
                XamlNodeType tokenType = xamlNode.TokenType;

                switch(tokenType)
                {
                    /////////////////////////////////////////////////////////////
                    //
                    // These XamlNode types will prevent us from using the TypeConverter syntax.

                    // Simple property setting is not allowed.
                    //  No: <SolidColorBrush Opacity="0.5">Red</SolidColorBrush>
                    case XamlNodeType.Property:
                        BamlAttributeUsage attributeUsage = ((XamlPropertyNode)xamlNode).AttributeUsage;

                        if( attributeUsage == BamlAttributeUsage.XmlSpace )
                        {
                            // An exception: xml:space is tolerated.  ParserContext
                            //  is set, and text processing is handled accordingly, but
                            //  the XmlAttributeProperties.XmlSpaceProperty attached DP
                            //  will not be set on this element.

                            // Example:
                            //  <SolidColorBrush xml:space="preserve">   Red   </SolidColorBrush>
                            //  BrushConverter will get "   Red   " instead of the collapsed "Red",
                            //  but the SolidColorBrush will not get the XmlSpaceProperty attached DP.

                            return false;
                        }
                        return true;

                    // None of the PropertyWithType nodes are allowed in conjuction with a TypeConverter
                    case XamlNodeType.PropertyWithType:

                    // This node represents a simple MarkupExtension value and should not be allowed in
                    // conjuntion with a TypeConverter
                    case XamlNodeType.PropertyWithExtension:

                    // No complex property (or variant) is allowed in TypeConverter syntax
                    case XamlNodeType.PropertyComplexStart:
                    case XamlNodeType.PropertyComplexEnd:
                    case XamlNodeType.PropertyArrayStart:
                    case XamlNodeType.PropertyArrayEnd:
                    case XamlNodeType.PropertyIListStart:
                    case XamlNodeType.PropertyIListEnd:
                    case XamlNodeType.PropertyIDictionaryStart:
                    case XamlNodeType.PropertyIDictionaryEnd:

                    // Events may not be set on a TypeConverter-created object
                    case XamlNodeType.RoutedEvent:
                    case XamlNodeType.ClrEvent:

                    // These are involved in an entirely different kind of delay creation.
                    case XamlNodeType.ConstructorParametersStart:
                    case XamlNodeType.ConstructorParametersEnd:
                    case XamlNodeType.ConstructorParameterType:

                    // Content use is mutually exclusive with TypeConverter use -
                    //  By this point we've decided Text will be Content, not TypeConverter input.
                    case XamlNodeType.ContentProperty:

                    // We don't know what these are, so we can't guarantee that
                    //  TypeConvert will work.
                    case XamlNodeType.Unknown:
                    case XamlNodeType.UnknownTagStart:
                    case XamlNodeType.UnknownTagEnd:
                    case XamlNodeType.UnknownAttribute:

                    // Definition tags are non-text content and breaks the
                    //  text content only rule for TypeConverter information.
                    case XamlNodeType.DefTag:

                    // This means a tag like <Linebreak /> where End immediately follows Start.
                    //  There's nothing for a TypeConverter to do here.
                    case XamlNodeType.ElementEnd:

                        return true;

                    /////////////////////////////////////////////////////////////
                    //
                    // These tags are tolerated while looking for TypeConverter info

                    // This is expected when we hit the end of the start tag.
                    case XamlNodeType.EndAttributes:

                    // XML namespace definitions are OK
                    case XamlNodeType.XmlnsProperty:

                    // When XMLNS is used to define reference to assembly, that causes
                    //  a PIMapping node to be generated under the covers.  "PIMapping" used
                    //  to be their own tags in the markup, but were removed from the
                    //  XAML syntax.  Rather than changing the parser to actually match the
                    //  new XAML syntax, this magic-mapping is done instead so we pretend
                    //  to use the new XAML syntax while we're still really doing the old
                    //  things under the covers.
                    case XamlNodeType.PIMapping:

                    // This property affects what we do with an object after we've
                    //  created the instance.  It does not affect whether we use
                    //  a TypeConverter to create it.
                    case XamlNodeType.PresentationOptionsAttribute:
                        return false;

                    /////////////////////////////////////////////////////////////
                    //
                    // XAML Directive Attributes - a whole funhouse of special cases.

                    case XamlNodeType.DefAttribute:
                        string nodeName = ((XamlDefAttributeNode)xamlNode).Name;

                        if( nodeName == DefinitionRuntimeName )
                        {
                            // In an ideal world, x:Name would be tolerated.
                            // Unfortunately, due to a collision with how
                            //  RuntimeIdentifierPropertyAttribute is implemented,
                            //  x:Name breaks our ability to use TypeConverter.
                            return true;
                        }
                        else if( nodeName == DefinitionTypeArgs )
                        {
                            // x:TypeArguments tells the parser the Type to use
                            //  when creating an instance of a generic.  (IList<T>, etc.)
                            // We do not have any way of passing this type information
                            //  into TypeConverter creation, so this is not
                            //  supported in the TypeConverter syntax.
                            return true;
                        }
                        else if( nodeName == DefinitionFieldModifier ||
                                 nodeName == DefinitionClassModifier )
                        {
                            // These attributes are used to modify the code access
                            //  level of generated classes.  (Internal/Public,etc.)
                            // We cannot use a TypeConverter to create an instance
                            //  of a class defined in a generated file.
                            return true;
                        }
                        return false;

                    // This is a x:Key="{x:Type Foobar}" when the Foobar type
                    //  could not be resolved at parse/compile time.  (Or else it
                    //  would be KeyElementStart(TypeExtension)/KeyElementEnd.)
                    // This does not prevent us from using TypeConverter.
                    case XamlNodeType.DefKeyTypeAttribute:
                        return false;

                    /////////////////////////////////////////////////////////////
                    //
                    // Nodes we don't expect to see.

                    // These node types were supposed to be handled by the caller.
                    case XamlNodeType.ElementStart:
                    case XamlNodeType.Text:
                    case XamlNodeType.KeyElementStart:
                    case XamlNodeType.KeyElementEnd:

                    // These are not handled by immediate caller, but we expect
                    //  them to have been handled further upstream.
                    case XamlNodeType.DocumentStart:
                    case XamlNodeType.DocumentEnd:
                    case XamlNodeType.Comment:
                    case XamlNodeType.LiteralContent:
                    case XamlNodeType.ProcessingInstruction:

                    // If not any of the above node types, then we have a new
                    //  XamlNode type that must be evaluated for TypeConverter
                    //  compatibility.

                    default:
                        Debug.Assert(false,"State machine checking for TypeConverter syntax has encountered an unexpected XamlNode type " + tokenType);

                        // If we didn't expect it - assume it invalidates our ability
                        //  to use a TypeConverter.
                        return true;
                }

            }

            //////////////////////////////////////////////////////////////////////////

            private TypeConverterDecisionState _typeConverterDecisionState = TypeConverterDecisionState.Uninitialized;
            private int _typeConverterCandidateIndex = 0;
            private string _typeConverterTextWrittenAndNotProcessed = null;

            ArrayList _xamlNodes = new ArrayList(10); // array of Nodes for Read to return.
            int _nodeIndex = 0; //index of next Node in the _xamlNodes buffer to return.
            int _insertionIndex = -1; // index of the node where the insertion was marked to begin.
            int _currentInsertionIndex = -1; // index of the node where the next insertion should happen.
        }


        /// <summary>
        /// given the TextNode sees if there is text and if so
        /// collapses and writes the TextNode Record.
        /// return true if a textNode was added,
        /// the stripNextTextNodesLeadingWhitespace is set
        /// based on the Text. if no text its value is set
        /// to whatever the textFlow was set to previously.
        /// </summary>
        bool CollapseAndAddTextNode(TextFlowStackData textFlowData, bool stripAllRightWhitespace)
        {

            bool addedText = false;

            if (null != textFlowData.TextNode && textFlowData.TextNode.Text.Length > 0)
            {
                string collapsedText;
                bool endedWithWhitespace;

                collapsedText = CollapseText(textFlowData.TextNode.Text,
                    textFlowData.StripLeadingSpaces,
                    stripAllRightWhitespace, textFlowData.XmlSpaceIsPreserve, out endedWithWhitespace);

                // if got any text back write it out.
                textFlowData.TextNode.UpdateText(collapsedText);
                if (collapsedText.Length > 0)
                {
                    TokenReaderNodeCollection.Add(textFlowData.TextNode);
                    textFlowData.TextNode = null;
                    addedText = true;
                }

                // update StripLeadingSpaces based on if this
                // string ending with whitespace.
                textFlowData.StripLeadingSpaces = endedWithWhitespace;
            }

            return addedText;
        }


        /// <summary>
        /// Called whenever items should be added to the nodes collections.
        /// </summary>
        void AddNodeToCollection(XamlNode xamlNode)
        {
            AddNodeToCollection(xamlNode, false, false);
        }

        void AddNodeToCollection(XamlNode xamlNode, bool insert, bool insertAtStart)
        {

            bool addNodeToBuffer = true; // set to false if need to do TextProcessing.
            bool textNodeAdded = false; // need to track if the textNode passed in is going to be in the baml or not

            // if there is a preCount we currently only allow
            // text within,
            bool preserveText = false;

            // Do any text Node handling for cached text of if this is
            // a textNode
            // !! should always be something on the stack since we push the Root.
            TextFlowStackData textFlowData = (TextFlowStackData)_textFlowStack.Peek();
            Debug.Assert(null != textFlowData, "No data for TextFlow");

            // do any processing for Text whitespace.
            switch (xamlNode.TokenType)
            {
                // possible that top-level text may not have been processed yet so
                // if get an EndDocument check the buffer

                case XamlNodeType.DocumentEnd:
                    {
                        // possible to have Text in the Buffer for the
                        // EndDocument if there is text at the root.
                        textNodeAdded = CollapseAndAddTextNode(textFlowData, true /* stripAllRightWhitespace */);
                        break;
                    }

                // process as begin tag
                // could be start of a new flow or inline.
                case XamlNodeType.ElementStart:
                    {
                        XamlElementStartNode elementNode = (XamlElementStartNode)xamlNode;
                        Type typeRightTag = elementNode.ElementType;
                        Debug.Assert(null != typeRightTag, "ElementType not yet assigned to Node");

                        // get whitespace type for tag
                        bool rightTagTrimSurroundingWhitespace = GetTrimSurroundingWhitespace(typeRightTag);
                        bool stripAllRightWhitespace = rightTagTrimSurroundingWhitespace;

                        // now have enough information on what to do with
                        // any textFlowData. If there isn't any
                        // text set the stripNextTextNodesLeadingWhitespace = to the current
                        // value.
                        // so inline the whitespace still gets stripped on the next
                        // tag <Button><B> This is Text </B> or should it be false?
                        // for block and inline block the stripNextTextNodesLeadingWhitespace
                        // will be updated based on if block or Inline.

                        textNodeAdded = CollapseAndAddTextNode(textFlowData, stripAllRightWhitespace);
                        textFlowData.StripLeadingSpaces = rightTagTrimSurroundingWhitespace;
                        TextFlowStackData flowData = new TextFlowStackData();
                        TextFlowStack.Push(flowData);

                        break;
                    }
                // process as text buffer
                case XamlNodeType.Text:
                {
                    // If we already have a text node, and we've encountered an ignorable element
                    // like a comment, then just append the new text to the existing text node
                    if (textFlowData.EncounteredIgnorableTag && textFlowData.TextNode != null)
                    {
                        textFlowData.TextNode.UpdateText(textFlowData.TextNode.Text +
                                                         ((XamlTextNode)xamlNode).Text);
                        textFlowData.EncounteredIgnorableTag = false;
                        addNodeToBuffer = false;
                    }
                    else
                    {
                        // !!! important set addNodeToBuffer to false so passed in text isn't added yet
                        // to the outputBuffer.
                        addNodeToBuffer = false;
                        textNodeAdded = CollapseAndAddTextNode(textFlowData, false /* stripAllRightWhitespace */);

                        // set the new text as the TextRun and update leading spaces
                        textFlowData.TextNode = (XamlTextNode)xamlNode;

                        // set the Prserve value check for if the xml:space = "preserve"
                        if (preserveText || (null != ParserContext.XmlSpace &&
                            ParserContext.XmlSpace.Equals("preserve")))
                        {
                            textFlowData.XmlSpaceIsPreserve = true;
                        }
                        else
                        {
                            textFlowData.XmlSpaceIsPreserve = false;
                        }
                    }
                }
                break;

                // process as end tag
                case XamlNodeType.ElementEnd:
                    {
                        // if this is not inline element then else it is
                        // okay to leave them.

                        // strip any trailing space unless this is an end to an inline tag.
                        bool stripAllRightWhitespace;
                        if (textFlowData.InlineCount > 0)
                        {
                            stripAllRightWhitespace = false;
                        }
                        else
                        {
                            stripAllRightWhitespace = true;
                        }

                        textNodeAdded = CollapseAndAddTextNode(textFlowData, stripAllRightWhitespace);

                        // if this is inline then decrement the inline count
                        // else pop the stack
                        if (textFlowData.InlineCount > 0)
                        {
                            textFlowData.InlineCount--;
                        }
                        else
                        {
                            TextFlowStack.Pop();
                        }
                    }
                    break;

                // treat as tags are not there but keep track for flows.
                // i.e. don't process text in buffer on either start tag.
                case XamlNodeType.PropertyComplexStart:
                case XamlNodeType.PropertyArrayStart:
                case XamlNodeType.PropertyIListStart:
                case XamlNodeType.PropertyIDictionaryStart:
                    {
                        // If we've accumulated whitespace text before the start of a
                        // property tag, then empty out that text, since property tags
                        // shouldn't cause whitespace text content to be realized.
                        if (textFlowData.TextNode != null)
                        {
                            if (IsWhitespace(textFlowData.TextNode.Text))
                            {
                                textFlowData.TextNode.UpdateText(string.Empty);
                            }
                            else
                            {
                                textNodeAdded = CollapseAndAddTextNode(textFlowData, /*stripAllRightWhitespace:*/true);
                            }
                        }
                        TextFlowStackData flowData = new TextFlowStackData();
                        TextFlowStack.Push(flowData);
                        break;
                    }

                // pop the stack shouldn't be any text to process.
                case XamlNodeType.PropertyComplexEnd:
                case XamlNodeType.PropertyArrayEnd:
                case XamlNodeType.PropertyIListEnd:
                case XamlNodeType.PropertyIDictionaryEnd:
                    Debug.Assert(0 == textFlowData.InlineCount, "Text stack still has an inline count");
                    textNodeAdded = CollapseAndAddTextNode(textFlowData, /*stripAllRightWhitespace:*/true);
                    _textFlowStack.Pop();
                    break;

                default:
                    break;
            }


            //If we aren't waiting to figure out when the textnode ends
            if (addNodeToBuffer)
            {
                //If a textnode was added to the baml (whitespace collapsing, etc... may cause textnodes to not be put into baml)
                //then we want to make sure that the parent of the textnode has the correct number of children.  The parent may either have
                //one or n children.  We also enforce that content can't be split up by property elements.
                if (textNodeAdded)
                {
                    bool isPropertyStartNode = false;

                    // ElementStart and Property*Start (which are XML Elements) each
                    // push a new context frame.  So for those, look in the parent.
                    ElementContextStackData textContext;
                    switch (xamlNode.TokenType)
                    {
                        case XamlNodeType.ElementStart:
                            textContext = ParentContext;
                            break;

                        case XamlNodeType.PropertyComplexStart:
                        case XamlNodeType.PropertyArrayStart:
                        case XamlNodeType.PropertyIListStart:
                        case XamlNodeType.PropertyIDictionaryStart:
                            textContext = ParentContext;
                            isPropertyStartNode = true;
                            break;
                        default:
                            textContext = CurrentContext;
                            break;
                    }

                    // Verify... "throw"s if we are "After" Content and
                    // returns false if there are multiple elements but
                    // the property is not a container.
                    if (!VerifyContentPropertySeesAnElement(textContext))
                    {
                        //only allow the 2nd (thru Nth) add if ContentPropertyInfo is a Collection (IList, etc...)
                        //Would be nicer to get real string here, but that would require more caching during the non-error cases.
                        throw new InvalidOperationException(SR.Get(SRID.ParserCanOnlyHaveOneChild,
                            textContext.ContextDataType.Name /* Parent */,
                            (textFlowData.TextNode == null ? "" : textFlowData.TextNode.Text) /* Child */));
                    }

                    if(isPropertyStartNode)
                        textContext.ContentParserState = ParsingContent.After;
                }

                if (insert)
                {
                    if (insertAtStart)
                    {
                        TokenReaderNodeCollection.InsertAtStartMark(xamlNode);
                    }
                    else
                    {
                        TokenReaderNodeCollection.InsertAtCurrentMark(xamlNode);
                    }
                }
                else
                {
                    TokenReaderNodeCollection.Add(xamlNode);
                }
            }
        }


        #endregion XamlNodeCollection

        #region Properties

        /// <summary>
        /// TextFlowHelper parser is using to handle whitespace.
        /// </summary>
        XamlNodeCollectionProcessor TokenReaderNodeCollection
        {
            get { return _xamlNodeCollectionProcessor; }
        }

        /// <summary>
        /// Name of the class that holds the xml: attribute DPs (e.g. xml:Lang)
        /// </summary>
        string XmlAttributesFullName
        {
            get { return "System.Windows.Markup.XmlAttributeProperties"; }
        }

        /// <summary>
        ///  CurrentContext for the node processing
        /// </summary>
        ElementContextStackData CurrentContext
        {
            get { return (ElementContextStackData)ElementContextStack.CurrentContext; }
        }

        /// <summary>
        /// Complex properties belonging to the ParentContext for the node we are processing
        /// </summary>
        HybridDictionary CurrentProperties
        {
            get
            {
                ElementContextStackData stackData = (ElementContextStackData)ElementContextStack.CurrentContext;
                return stackData.ComplexProperties;
            }
        }

        /// <summary>
        /// Complex properties belonging to the ParentContext for the node we are processing
        /// </summary>
        HybridDictionary ParentProperties
        {
            get
            {
                ElementContextStackData stackData = (ElementContextStackData)ElementContextStack.ParentContext;
                return stackData.ComplexProperties;
            }
        }

        /// <summary>
        /// ParentContext for the node we are processing
        /// </summary>
        ElementContextStackData ParentContext
        {
            get { return (ElementContextStackData)ElementContextStack.ParentContext; }
        }

        /// <summary>
        /// ElementContext stack
        /// </summary>
        ParserStack ElementContextStack
        {
            get { return _elementContextStack; }
        }


        /// <summary>
        /// Current Parsercontext for the node being processed
        /// </summary>
        ParserContext ParserContext
        {
            get { return _parserContext; }
        }

        /// <summary>
        /// GrandParentContext for the node we are processing
        /// </summary>
        ElementContextStackData GrandParentContext
        {
            get { return (ElementContextStackData)ElementContextStack.GrandParentContext; }
        }


        /// <summary>
        /// XamlTypeMapper associated with the Tokenizer
        /// Review!! should be able to get rid of this if break
        /// out resolution logic
        /// </summary>
        XamlTypeMapper XamlTypeMapper
        {
            get { return _parserContext.XamlTypeMapper; }
        }

        /// <summary>
        /// Instance of XamlParser associated with the tokenReader.  If there is no
        /// XamlParser, then the token reader is used to parse some subset of a Xaml,
        /// such as styles, which follow a less strict set of rules.  So checking
        /// _xamlParser == null is often used for relaxed rule checking.
        /// </summary>
        internal XamlParser ControllingXamlParser
        {
            get { return _xamlParser; }
            set { _xamlParser = value; }
        }

        /// <summary>
        /// TextFlowStack for the TextFlows associated with the parse
        /// </summary>
        Stack TextFlowStack
        {
            get { return _textFlowStack; }
        }

        // mimic the type/reflection behavior that KnownTypes does for built-in types
        private bool IsAssignableToIXmlSerializable(Type type)
        {
            if (_typeIXmlSerializable == null)
            {
#if PBTCOMPILER
                    // in PBT, make sure this is ReflectionOnly
                    Assembly asmXml = ReflectionHelper.GetAlreadyReflectionOnlyLoadedAssembly("SYSTEM.XML");
                    // System.Xml is only in loaded list if it is actually contained in app's link ref list
                    if (asmXml == null)
                        return false;
                    _typeIXmlSerializable = asmXml.GetType("System.Xml.Serialization.IXmlSerializable");
#else
                _typeIXmlSerializable = typeof(System.Xml.Serialization.IXmlSerializable);
#endif
            }
            return _typeIXmlSerializable.IsAssignableFrom(type);
        }

        private bool IsElementScopedAttribute(string attribName, string attributeLocalName, string attributeNamespaceUri)
        {
            return attribName.StartsWith("xmlns:", StringComparison.Ordinal) ||
                   attribName.Equals(XmlnsDeclaration) ||
                   attribName.Equals(XmlAttributeProperties.XmlSpaceString) ||
                   attribName.Equals(XmlAttributeProperties.XmlLangString) ||
                   IsAttributePresentationOptionsFreeze(attributeLocalName, attributeNamespaceUri);
        }



        #endregion Properties

        #region Data


        // private Data
        private const string XmlnsDeclaration = "xmlns";

        // XmlReader being used for the parse
        XmlReader _xmlReader;

        // LineInfo interface associated with the XmlReader
        IXmlLineInfo _xmlLineInfo;

        // Reference to XamlParser to call for Cached Lookups
        XamlParser _xamlParser;

        // Context stack for each Element
        ParserStack _elementContextStack = new ParserStack();

        // State of the parser
        ParserState _parseLoopState;

        // ParserContext for current node
        ParserContext _parserContext;

        // Collection of Nodes that are ready to be handed out
        XamlNodeCollectionProcessor _xamlNodeCollectionProcessor;

        // A markup-subclassed Type defined in one scope. If used in another scope, names under an
        // element instance of this type will not be allowed.
        Type _definitionScopeType;

        // Stack for holding onto the textFlow
        Stack _textFlowStack;

        // The parser used for expanding MarkupExtension syntax into a sequence of
        // XamlNodes.  This is shared between XamlReaderHelper and BamlWriter.
        MarkupExtensionParser _extensionParser;

        // True if we should keep on reading the xaml stream after processing the current token
        bool _readAnotherToken = false;

        // True if we are within the context of an Xml Data Island, e.g. XmlDataProvider
        protected int _xmlDataIslandDepth = -1;

        private Type _typeIXmlSerializable;
        #endregion Data
    }

    #region XamlPropertyFullName
    [DebuggerDisplay("{Fullname}")]
    internal class XamlPropertyFullName
    {
        Type   _ownerType;
        string _name;

        public XamlPropertyFullName(Type ownerType, string name)
        {
            _ownerType = ownerType;
            _name = name;
        }

        public string Name { get { return _name; } }
        public Type OwnerType { get { return _ownerType; } }

        public string FullName { get { return _ownerType.FullName + "." + _name; } }

        public override bool Equals(object o)
        {
            XamlPropertyFullName other = (XamlPropertyFullName)o;
            return (_ownerType == other.OwnerType && (0==string.CompareOrdinal(_name, other.Name)));
        }

        public override int GetHashCode()
        {
            return _ownerType.GetHashCode() ^ _name.GetHashCode();
        }
    }
    #endregion // XamlPropertyFullName

}
