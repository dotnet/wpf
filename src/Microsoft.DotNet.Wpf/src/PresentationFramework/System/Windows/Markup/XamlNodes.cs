// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***************************************************************************\
*
* Purpose:  Xaml Node definition class. Contains the different nodes
*           That can be returned by the XamlReader
*
\***************************************************************************/


using System;
using System.Xml;
using System.IO;
using System.Collections;
using System.ComponentModel;

using System.Diagnostics;
using System.Reflection;

#if !PBTCOMPILER

using System.Windows;
using System.Windows.Threading;

#endif

using MS.Utility;

#if PBTCOMPILER
namespace MS.Internal.Markup
#else
namespace System.Windows.Markup
#endif
{   
        #region XamlNodeTypeDefitions

        /// <summary>
        /// Identifier for XamlNodes
        /// </summary>
        internal enum XamlNodeType
        {
            /// <summary>
            /// Unknown Node
            /// </summary>
            Unknown,
            
            /// <summary>
            /// Start Document Node
            /// </summary>
            DocumentStart,

            /// <summary>
            /// End Document Node
            /// </summary>
            DocumentEnd,

            /// <summary>
            /// Start Element Node, which may be a CLR object or a DependencyObject
            /// </summary>
            ElementStart,

            /// <summary>
            /// End Element Node
            /// </summary>
            ElementEnd,

            /// <summary>
            /// Property Node, which may be a CLR property or a DependencyProperty
            /// </summary>
            Property,

            /// <summary>
            /// Complex Property Node
            /// </summary>
            PropertyComplexStart,

            /// <summary>
            /// End Complex Property Node
            /// </summary>
            PropertyComplexEnd,

            /// <summary>
            /// Start Array Property Node
            /// </summary>
            PropertyArrayStart,

            /// <summary>
            /// End Array Property Node
            /// </summary>
            PropertyArrayEnd,

            /// <summary>
            /// Star IList Property Node
            /// </summary>
            PropertyIListStart,

            /// <summary>
            /// End IListProperty Node
            /// </summary>
            PropertyIListEnd,

            /// <summary>
            /// Start IDictionary Property Node
            /// </summary>
            PropertyIDictionaryStart,

            /// <summary>
            /// End IDictionary Property Node
            /// </summary>
            PropertyIDictionaryEnd,

            /// <summary>
            /// A property whose value is a simple MarkupExtension object
            /// </summary>
            PropertyWithExtension,

            /// <summary>
            /// A property whose value is a Type object
            /// </summary>
            PropertyWithType,

            /// <summary>
            /// LiteralContent Node
            /// </summary>
            LiteralContent,

            /// <summary>
            /// Text Node
            /// </summary>
            Text,

            /// <summary>
            /// RoutedEventNode
            /// </summary>
            RoutedEvent,

            /// <summary>
            /// ClrEvent Node
            /// </summary>
            ClrEvent,

            /// <summary>
            /// XmlnsProperty Node
            /// </summary>
            XmlnsProperty,

            /// <summary>
            /// XmlAttribute Node
            /// </summary>
            XmlAttribute,

            /// <summary>
            /// Processing Intstruction Node
            /// </summary>
            ProcessingInstruction,

            /// <summary>
            /// Comment Node
            /// </summary>
            Comment,

            /// <summary>
            /// DefTag Node
            /// </summary>
            DefTag,       

            /// <summary>
            /// DefAttribute Node
            /// </summary>
            DefAttribute,  

            /// <summary>
            /// PresentationOptionsAttribute Node
            /// </summary>
            PresentationOptionsAttribute,              

            /// <summary>
            /// x:Key attribute that is resolved to a Type
            /// </summary>
            DefKeyTypeAttribute,
            
            /// <summary>
            /// EndAttributes Node
            /// </summary>
            EndAttributes,

            /// <summary>
            /// PI xml - clr namespace mapping
            /// </summary>
            PIMapping,
            
            /// <summary>
            /// Unknown tag
            /// </summary>
            UnknownTagStart,
            
            /// <summary>
            /// Unknown tag
            /// </summary>
            UnknownTagEnd,

            /// <summary>
            /// Unknown attribute
            /// </summary>
            UnknownAttribute,

            /// <summary>
            /// Start of an element tree used
            /// to identify a key in an IDictionary.
            /// </summary>
            KeyElementStart,

            /// <summary>
            /// End of an element tree used
            /// to identify a key in an IDictionary.
            /// </summary>
            KeyElementEnd,

            /// <summary>
            /// Start of a section that contains one or more constructor parameters
            /// </summary>
            ConstructorParametersStart,
            
            /// <summary>
            /// Start of a section that contains one or more constructor parameters
            /// </summary>
            ConstructorParametersEnd,
            
            /// <summary>
            /// Constructor parameter that has been resolved to a Type at compile time.
            /// </summary>
            ConstructorParameterType,

            /// <summary>
            /// Node to set the content property
            /// </summary>
            ContentProperty,
        }

        /// <summary>
        /// Base Node in which all others derive
        /// </summary>
        internal class XamlNode
        {
            /// <summary>
            /// Constructor
            /// </summary>
            internal XamlNode(
                XamlNodeType tokenType,
                int          lineNumber,
                int          linePosition,
                int          depth)
            {
                _token = tokenType;
                _lineNumber = lineNumber;
                _linePosition = linePosition;
                _depth = depth;
            }

            /// <summary>
            /// Token Type of the Node
            /// </summary>
            internal XamlNodeType TokenType  
            { 
                get { return _token; }
            }

            /// <summary>
            /// LineNumber of Node in File
            /// </summary>
            internal int LineNumber 
            {
                get { return _lineNumber; }
            }

            /// <summary>
            /// LinePosition of Node in File
            /// </summary>
            internal int LinePosition 
            {
                get { return _linePosition; }
            }

            /// <summary>
            /// Depth of the Node
            /// </summary>
            internal int Depth 
            {
                get { return  _depth; }
            }

            /// <summary>
            /// An array of xamlnodes that represent the start of scoped portions of the xaml file
            /// </summary>
            internal static XamlNodeType[] ScopeStartTokens = new XamlNodeType[]{
                                                                   XamlNodeType.DocumentStart,
                                                                   XamlNodeType.ElementStart,
                                                                   XamlNodeType.PropertyComplexStart,
                                                                   XamlNodeType.PropertyArrayStart,
                                                                   XamlNodeType.PropertyIListStart,
                                                                   XamlNodeType.PropertyIDictionaryStart,
                                                               };
            
            /// <summary>
            /// An array of xamlnodes that represent the end of scoped portions of the xaml file
            /// </summary>
            internal static XamlNodeType[] ScopeEndTokens = new XamlNodeType[]{
                                                                   XamlNodeType.DocumentEnd,
                                                                   XamlNodeType.ElementEnd,
                                                                   XamlNodeType.PropertyComplexEnd,
                                                                   XamlNodeType.PropertyArrayEnd,
                                                                   XamlNodeType.PropertyIListEnd,
                                                                   XamlNodeType.PropertyIDictionaryEnd,
                                                               };

            XamlNodeType _token;
            int _lineNumber;
            int _linePosition;
            int _depth;
        }


        /// <summary>
        /// XamlDocument start node
        /// </summary>
        internal class XamlDocumentStartNode : XamlNode
        {
            /// <summary>
            /// Constructor
            /// </summary>
            internal XamlDocumentStartNode(
                int lineNumber,
                int linePosition,
                int depth)
                : base (XamlNodeType.DocumentStart,lineNumber,linePosition,depth)
            {
            }
        }

        /// <summary>
        /// XamlDocument end node
        /// </summary>
        internal  class XamlDocumentEndNode : XamlNode
        {
            /// <summary>
            /// Constructor
            /// </summary>
            /// <ExternalAPI/>
            internal XamlDocumentEndNode(
                int lineNumber,
                int linePosition,
                int depth)
                : base (XamlNodeType.DocumentEnd,lineNumber,linePosition,depth)
            {
            }
        }

        /// <summary>
        /// Xaml Text Node
        /// </summary>
        [DebuggerDisplay("Text:{_text}")]
        internal class XamlTextNode : XamlNode
       {
            /// <summary>
            /// Constructor
            /// </summary>
            internal XamlTextNode(
                int    lineNumber,
                int    linePosition,
                int    depth,
                string textContent,
                Type   converterType)
                : base (XamlNodeType.Text,lineNumber,linePosition,depth)
            {
                _text = textContent;
                _converterType = converterType;
            }

            /// <summary>
            /// Text for the TextNode
            /// </summary>
            internal string Text
            {
                get { return _text; }
            }

            /// <summary>
            /// Type of Converter to be used to convert this text value into an object
            /// </summary>
            internal Type ConverterType
            {
                get { return _converterType; }
            }

            /// <summary>
            /// internal function so Tokenizer can just update a text
            /// node after whitespace processing
            /// </summary>
            internal void UpdateText(string text)
            {
                _text = text;
            }

            string _text;
            Type   _converterType = null;
        }


        /// <summary>
        /// Base class for XamlPropertyNode and XamlPropertyComplexStartNode
        /// </summary>
        [DebuggerDisplay("Prop:{_typeFullName}.{_propName}")]
       internal class XamlPropertyBaseNode : XamlNode
        {
            /// <summary>
            /// Constructor
            /// </summary>
            internal XamlPropertyBaseNode(
                XamlNodeType   token,
                int         lineNumber,
                int         linePosition,
                int         depth,
                object      propertyMember,    // DependencyProperty or MethodInfo or PropertyInfo
                string      assemblyName,
                string      typeFullName,
                string      propertyName)
                : base (token,
                        lineNumber,
                        linePosition,
                        depth)
            {
                if (typeFullName == null)
                {
                    throw new ArgumentNullException("typeFullName");
                }
                if (propertyName == null)
                {
                    throw new ArgumentNullException("propertyName");
                }
                
                _propertyMember = propertyMember;
                _assemblyName = assemblyName;
                _typeFullName = typeFullName;
                _propName = propertyName;
            }
            
#if PBTCOMPILER
            /// <summary>
            /// PropertyInfo for the property for the Node.  This may be null.
            /// </summary>
            internal PropertyInfo PropInfo
            {
                get { return _propertyMember as PropertyInfo;}
            }
#endif

            /// <summary>
            /// Assembly of the type that owns or has declared the Property
            /// </summary>
            internal string AssemblyName {
                get { return _assemblyName; }
            }

            /// <summary>
            /// TypeFullName of type that owns or has declared the Property
            /// </summary>
            internal string TypeFullName {
                get { return _typeFullName; }
            }
            
            /// <summary>
            /// Name of the Property
            /// </summary>
            internal string PropName {
                get { return _propName;}
            }

            /// <summary>
            /// Type of the owner or declarer of this Property
            /// </summary>
            internal Type PropDeclaringType
            {
                get 
                { 
                    // Lazy initialize this to avoid addition reflection if it
                    // is not needed.
                    if (_declaringType == null && _propertyMember != null)
                    {
                        _declaringType = XamlTypeMapper.GetDeclaringType(_propertyMember);
                    }
                    return _declaringType;
                }
            }
            
            /// <summary>
            /// Valid Type of the Property
            /// </summary>
            internal Type PropValidType
            {
                get 
                { 
                    // Lazy initialize this to avoid addition reflection if it
                    // is not needed.
                    if (_validType == null)
                    {
                        _validType = XamlTypeMapper.GetPropertyType(_propertyMember);
                    }
                    return _validType;
                }
            }

             /// <summary>
            /// Property methodinfo or propertyinfo
            /// </summary>
            internal object PropertyMember
            {
                get  { return _propertyMember; }
            }


            object     _propertyMember;
            string     _assemblyName;
            string     _typeFullName;
            string     _propName;
            Type       _validType;
            Type       _declaringType;
        }

        /// <summary>
        /// Xaml Complex Property Node, which is a property of the
        /// form /<Classname.Propertyname/>.  This can pertain to any type of object.
        /// </summary>
        internal class XamlPropertyComplexStartNode : XamlPropertyBaseNode
        {
            /// <summary>
            /// Constructor
            /// </summary>
            internal XamlPropertyComplexStartNode(
                int         lineNumber,
                int         linePosition,
                int         depth,
                object      propertyMember,    // DependencyProperty or MethodInfo or PropertyInfo
                string      assemblyName,
                string      typeFullName,
                string      propertyName)
                : base (XamlNodeType.PropertyComplexStart,
                        lineNumber,
                        linePosition,
                        depth,
                        propertyMember,
                        assemblyName,
                        typeFullName,
                        propertyName)
            {
            }
            
            /// <summary>
            /// Internal Constructor
            /// </summary>
            internal XamlPropertyComplexStartNode(
                XamlNodeType   token,
                int            lineNumber,
                int            linePosition,
                int            depth,
                object         propertyMember,    // DependencyProperty or MethodInfo or PropertyInfo
                string         assemblyName,
                string         typeFullName,
                string         propertyName)
                : base (token,
                        lineNumber,
                        linePosition,
                        depth,
                        propertyMember,
                        assemblyName,
                        typeFullName,
                        propertyName)
            {
            }
        }

        /// <summary>
        /// Xaml Complex Property Node, which is a property of the
        /// form /<Classname.Propertyname/>.  This can pertain to any type of object.
        /// </summary>
        internal class XamlPropertyComplexEndNode : XamlNode
        {
            /// <summary>
            /// Contstructor
            /// </summary>
            internal XamlPropertyComplexEndNode(
                int lineNumber,
                int linePosition,
                int depth)
                : base (XamlNodeType.PropertyComplexEnd,lineNumber,linePosition,depth)
            {
            }

            /// <summary>
            /// Contstructor
            /// </summary>
            internal XamlPropertyComplexEndNode(
                XamlNodeType   token,
                int lineNumber,
                int linePosition,
                int depth)
                : base (token,lineNumber,linePosition,depth)
            {
            }
        }

        /// <summary>
        /// Xaml Property Node, whose value is a simple MarkupExtension.
        /// </summary>
        internal class XamlPropertyWithExtensionNode : XamlPropertyBaseNode
        {
            internal XamlPropertyWithExtensionNode(
                int      lineNumber,
                int      linePosition,
                int      depth,
                object   propertyMember,    // DependencyProperty, MethodInfo or PropertyInfo
                string   assemblyName,
                string   typeFullName,
                string   propertyName,
                string   value,
                short    extensionTypeId,
                bool     isValueNestedExtension,
                bool     isValueTypeExtension) : base(XamlNodeType.PropertyWithExtension,
                                                      lineNumber,
                                                      linePosition,
                                                      depth,
                                                      propertyMember,  
                                                      assemblyName,
                                                      typeFullName,
                                                      propertyName)
            {
                _value = value;
                _extensionTypeId = extensionTypeId;
                _isValueNestedExtension = isValueNestedExtension;
                _isValueTypeExtension = isValueTypeExtension;
                _defaultTargetType = null;
            }

            internal short ExtensionTypeId
            {
                get { return _extensionTypeId; }
            }

            internal string Value
            {
                get { return _value; }
            }

            internal bool IsValueNestedExtension
            {
                get { return _isValueNestedExtension; }
            }

            internal bool IsValueTypeExtension
            {
                get { return _isValueTypeExtension; }
            }

            internal Type DefaultTargetType
            {
                get { return _defaultTargetType; }
                set { _defaultTargetType = value; }
            }

            short _extensionTypeId;
            string _value;
            bool _isValueNestedExtension;
            bool _isValueTypeExtension;
            Type _defaultTargetType;
        }

        /// <summary>
        /// Xaml Property Node, which can be a DependencyProperty, CLR property or
        /// hold a reference to a static property set method.
        /// </summary>
        internal class XamlPropertyNode : XamlPropertyBaseNode
        {
            /// <summary>
            /// Constructor
            /// </summary>
            internal XamlPropertyNode(
                int                lineNumber,
                int                linePosition,
                int                depth,
                object             propertyMember,    // DependencyProperty, MethodInfo or PropertyInfo
                string             assemblyName,
                string             typeFullName,
                string             propertyName,
                string             value,
                BamlAttributeUsage attributeUsage,
                bool               complexAsSimple) : base (XamlNodeType.Property,
                                                            lineNumber,
                                                            linePosition,
                                                            depth,
                                                            propertyMember,  
                                                            assemblyName,
                                                            typeFullName,
                                                            propertyName)
            {
                _value = value;
                _attributeUsage = attributeUsage;
                _complexAsSimple = complexAsSimple;
            }

            internal XamlPropertyNode(
                int lineNumber,
                int linePosition,
                int depth,
                object propertyMember,    // DependencyProperty, MethodInfo or PropertyInfo
                string assemblyName,
                string typeFullName,
                string propertyName,
                string value,
                BamlAttributeUsage attributeUsage,
                bool complexAsSimple,
                bool isDefinitionName) : this(lineNumber,
                                              linePosition,
                                              depth,
                                              propertyMember,
                                              assemblyName,
                                              typeFullName,
                                              propertyName,
                                              value,
                                              attributeUsage,
                                              complexAsSimple)
            {
                _isDefinitionName = isDefinitionName;
            }

#if PBTCOMPILER
            internal bool IsDefinitionName
            {
                get { return _isDefinitionName; }
            }
#endif

            /// <summary>
            /// Value for the attribute
            /// </summary>
            internal string Value
            {
                get { return _value;}
            }

            /// <summary>
            /// Change the value stored in the xaml node.  For internal use only.
            /// </summary>
            internal void SetValue(string value)
            {
                _value = value;
            }

            ///<summary>
            /// Return the declaring type to use when resolving the type converter
            /// or serializer for this property value.
            ///</summary>
            internal Type ValueDeclaringType
            {
                get
                {
                    if (_valueDeclaringType == null)
                    {
                        return PropDeclaringType;
                    }
                    else
                    {
                        return _valueDeclaringType;
                    }
                }
                set { _valueDeclaringType = value; }
            }

            ///<summary>
            /// Return the property name to use when resolving the type converter
            /// or serializer for this property value.  This may be different than
            /// the actual property name for this property
            ///</summary>
            internal string ValuePropertyName
            {
                get
                {
                    if (_valuePropertyName == null)
                    {
                        return PropName;
                    }
                    else
                    {
                        return _valuePropertyName;
                    }
                }
                set { _valuePropertyName = value; }
            }

            ///<summary>
            /// Return the property type to use when resolving the type converter
            /// or serializer for this property value.  This may be different than
            /// the actual property type for this property.
            ///</summary>
            internal Type ValuePropertyType
            {
                get
                {
                    if (_valuePropertyType == null)
                    {
                        return PropValidType;
                    }
                    else
                    {
                        return _valuePropertyType;
                    }
                }
                set { _valuePropertyType = value; }
            }

            ///<summary>
            /// Return the property member info to use when resolving the type converter
            /// or serializer for this property value.  This may be different than
            /// the actual property member info for this property.
            ///</summary>

            internal object ValuePropertyMember
            {
                get
                {
                    if (_valuePropertyMember == null)
                    {
                        return PropertyMember;
                    }
                    else
                    {
                        return _valuePropertyMember;
                    }
                }
                set { _valuePropertyMember = value; }
            }

            // Indicates if the valueId been explcitly set
            internal bool HasValueId
            {
                get { return _hasValueId; }
            }

            // This is either a known dependency property Id or a 
            // TypeId of the resolved owner type of a DP that is the
            // value of this property node.
            internal short ValueId
            {
                get { return _valueId; }
                set
                { 
                    _valueId = value;
                    _hasValueId = true;
                }
            }

            // If ValueId is a TypeId, this stores the name of the DP.
            internal string MemberName
            {
                get { return _memberName; }
                set { _memberName = value; }
            }

            // The type to resolve the DP value against if Onwer is not explicitly specified.
            internal Type DefaultTargetType
            {
                get { return _defaultTargetType; }
                set { _defaultTargetType = value; }
            }

            /// <summary>
            /// Gives the specific usage of this property
            /// </summary>
            /// <remarks>
            /// Some properties are not only set on an element, but have some other effects
            /// such as setting the xml:lang or xml:space values in the parser context.
            /// The AttributeUsage describes addition effects or usage for this property.
            /// </remarks>
            internal BamlAttributeUsage AttributeUsage
            {
                get { return _attributeUsage; }
            }

            /// <summary>
            /// A XamlPropertyNode is created from a property=value assignment, 
            /// or it may be created from a complex subtree which contains text, but is
            /// represented as a simple property.  In the latter case, ComplexAsSimple
            /// should be set to True.
            /// </summary>
            internal bool ComplexAsSimple
            {
                get { return _complexAsSimple; }
            }

            string             _value;
            BamlAttributeUsage _attributeUsage;
            bool               _complexAsSimple;
            bool               _isDefinitionName;

            // Variables for holding property info when this property's value is
            // resolved using the attributes of another property.
            Type               _valueDeclaringType;
            string             _valuePropertyName;
            Type               _valuePropertyType;
            object             _valuePropertyMember;
            bool               _hasValueId = false;
            short              _valueId = 0;
            string             _memberName = null;
            Type               _defaultTargetType;
        }

        /// <summary>
        /// Xaml Property Node, which can be a DependencyProperty, CLR property or
        /// hold a reference to a static property set method.
        /// </summary>
        internal  class XamlPropertyWithTypeNode : XamlPropertyBaseNode
        {
            /// <summary>
            /// Constructor
            /// </summary>
            internal XamlPropertyWithTypeNode(
                int      lineNumber,
                int      linePosition,
                int      depth,
                object   propertyMember,    // DependencyProperty, MethodInfo or PropertyInfo
                string   assemblyName,
                string   typeFullName,
                string   propertyName,
                string   valueTypeFullName,  // String value of type, in the form Namespace.Typename
                string   valueAssemblyName,  // Assembly name where type value is defined.
                Type     valueElementType,   // Actual type of the valueTypeFullname.
                string   valueSerializerTypeFullName,
                string   valueSerializerTypeAssemblyName) : 
                                         base (XamlNodeType.PropertyWithType,
                                               lineNumber,
                                               linePosition,
                                               depth,
                                               propertyMember,  
                                               assemblyName,
                                               typeFullName,
                                               propertyName)
            {
                _valueTypeFullname = valueTypeFullName;
                _valueTypeAssemblyName = valueAssemblyName;
                _valueElementType = valueElementType;
                _valueSerializerTypeFullName = valueSerializerTypeFullName;
                _valueSerializerTypeAssemblyName = valueSerializerTypeAssemblyName;
            }
                
            /// <summary>
            /// Value for the property's value, which should resolve to a Type.
            /// This is in the form Namespace.LocalTypeName
            /// </summary>
            internal string ValueTypeFullName
            {
                get { return _valueTypeFullname;}
            }
            
            /// <summary>
            /// Name of the assembly where the resolved value's type is declared.
            /// </summary>
            internal string ValueTypeAssemblyName
            {
                get { return _valueTypeAssemblyName;}
            }
            
            /// <summary>
            /// Cached value of the type of the value that is resolved at compile time
            /// </summary>
            internal Type ValueElementType
            {
                get { return _valueElementType;}
            }
            
            /// <summary>
            /// Name of the serializer to use when parsing an object that is of the
            /// type of ValueElementType
            /// </summary>
            internal string ValueSerializerTypeFullName
            {
                get { return _valueSerializerTypeFullName;}
            }

            /// <summary>
            /// Name of the assembly where the serializer to use when parsing an object 
            /// that is of the type of ValueElementType is declared.
            /// </summary>
            internal string ValueSerializerTypeAssemblyName
            {
                get { return _valueSerializerTypeAssemblyName;}
            }

            string    _valueTypeFullname;
            string    _valueTypeAssemblyName;
            Type      _valueElementType;
            string    _valueSerializerTypeFullName;
            string    _valueSerializerTypeAssemblyName;
        }

        /// <summary>
        /// Start of an Unknown attribute in Xaml.  This may be handled by
        /// a custom Serializer
        /// </summary>
        internal class XamlUnknownAttributeNode : XamlAttributeNode
        {
            /// <summary>
            /// Constructor
            /// </summary>
            internal XamlUnknownAttributeNode(
                int          lineNumber,
                int          linePosition,
                int          depth,
                string       xmlNamespace,
                string       name,
                string       value,
                BamlAttributeUsage attributeUsage)
                : base(XamlNodeType.UnknownAttribute,lineNumber,linePosition,
                       depth,value)
            {
                _xmlNamespace = xmlNamespace;
                _name = name;
                _attributeUsage = attributeUsage;
            }
             
            /// <summary>
            /// XmlNamespace associated with the unknown attribute
            /// </summary>
            internal string XmlNamespace
            {
                get { return _xmlNamespace;}
            }

            /// <summary>
            /// Name of the unknown property
            /// </summary>
            internal string Name
            {
                get { return _name;}
            }

#if PBTCOMPILER
            /// <summary>
            /// Gives the specific usage of this property
            /// </summary>
            /// <remarks>
            /// Some properties are not only set on an element, but have some other effects
            /// such as setting the xml:lang or xml:space values in the parser context.
            /// The AttributeUsage describes addition effects or usage for this property.
            /// </remarks>
            internal BamlAttributeUsage AttributeUsage
            {
                get { return _attributeUsage; }
            }

            /// <summary>
            /// Full TypeName of the unknown owner of this property
            /// </summary>
            internal string OwnerTypeFullName
            {
                get { return _ownerTypeFullName; }
                set { _ownerTypeFullName = value; }
            }

            string _ownerTypeFullName;
#endif
            string _xmlNamespace;

            string _name;

            BamlAttributeUsage _attributeUsage;
        }
     
        /// <summary>
        /// Xaml start element Node, which is any type of object
        /// </summary>
        [DebuggerDisplay("Elem:{_typeFullName}")]
        internal class XamlElementStartNode : XamlNode
        {
            /// <summary>
            /// Constructor
            /// </summary>
            internal XamlElementStartNode(
                int    lineNumber,
                int    linePosition,
                int    depth,
                string assemblyName,
                string typeFullName,
                Type   elementType,
                Type   serializerType)
                : this (XamlNodeType.ElementStart,
                        lineNumber,
                        linePosition,
                        depth,
                        assemblyName,
                        typeFullName,
                        elementType,
                        serializerType,
                        false /*isEmptyElement*/,
                        false /*needsDictionaryKey*/,
                        false /*isInjected*/)
            {
            }

            internal XamlElementStartNode(
                XamlNodeType tokenType,
                int          lineNumber,
                int          linePosition,
                int          depth,
                string       assemblyName,
                string       typeFullName,
                Type         elementType,
                Type         serializerType,
                bool         isEmptyElement,
                bool         needsDictionaryKey,
                bool         isInjected)
                : base (tokenType,lineNumber,linePosition,depth)
                
            {
                _assemblyName       = assemblyName;
                _typeFullName       = typeFullName;
                _elementType        = elementType;
                _serializerType     = serializerType;
                _isEmptyElement     = isEmptyElement;
                _needsDictionaryKey = needsDictionaryKey;
                _useTypeConverter   = false;
                IsInjected          = isInjected;
            }


            /// <summary>
            /// Assembly Object is in
            /// </summary>
            internal string AssemblyName
            {
                get { return _assemblyName; }
            }

            /// <summary>
            /// TypeFullName of the Object
            /// </summary>
            internal string TypeFullName
            {
                get { return _typeFullName; }
            }

            /// <summary>
            /// Type for the Object
            /// </summary>
            internal Type ElementType
            {
                get { return _elementType; }
            }
            
            /// <summary>
            /// Type of serializer to use when serializing or deserializing
            /// the element.  The serializer can be used to override the default
            /// deserialization action taken by the Avalon parser.
            /// If there is no custom serializer, then this is null.
            /// </summary>
            internal Type SerializerType
            {
                     get { return _serializerType; }
            }

            /// <summary>
            /// Type full name of serializer to use when serializing or deserializing
            /// the element.  The serializer can be used to override the default
            /// deserialization action taken by the Avalon parser.
            /// If there is no custom serializer, then this is an empty string.
            /// </summary>
            internal string SerializerTypeFullName
            {
                get 
                { 
                    return _serializerType == null ? 
                                   string.Empty :
                                   _serializerType.FullName;
                }
            }

            /// <summary>
            /// Whether we plan on creating an instance of this element via a
            /// TypeConverter using a Text node that follows.
            /// </summary>
            internal bool CreateUsingTypeConverter
            {
                get { return _useTypeConverter; }
                set { _useTypeConverter = value; }
            }

#if PBTCOMPILER

            // True if this element is the top level element in a dictionary, which
            // means that we have to have a key associated with it.
            internal bool NeedsDictionaryKey
            {
                get { return _needsDictionaryKey; }
            }
#endif

            internal bool IsInjected
            {
                get { return _isInjected; }
                set { _isInjected = value; }
            }

            string _assemblyName;
            string _typeFullName;
            Type   _elementType;
            Type   _serializerType;
            bool   _isEmptyElement;
            bool   _needsDictionaryKey;
            bool   _useTypeConverter;
            bool   _isInjected;
        }

        /// <summary>
        /// Start of a constructor parameters section
        /// </summary>
        internal class XamlConstructorParametersStartNode : XamlNode
        {
            /// <summary>
            /// Constructor
            /// </summary>
            internal XamlConstructorParametersStartNode(
                int lineNumber,
                int linePosition,
                int depth)
                : base (XamlNodeType.ConstructorParametersStart,lineNumber,linePosition,depth)
            {
            }
        }

        /// <summary>
        /// End of a constructor parameters section
        /// </summary>
        internal class XamlConstructorParametersEndNode : XamlNode
        {
            /// <summary>
            /// Constructor
            /// </summary>
            internal XamlConstructorParametersEndNode(
                int lineNumber,
                int linePosition,
                int depth)
                : base (XamlNodeType.ConstructorParametersEnd,lineNumber,linePosition,depth)
            {
            }
        }

#if PBTCOMPILER
        /// <summary>
        /// Constructor parameter that is a Type object
        /// </summary>
        internal class XamlConstructorParameterTypeNode : XamlNode
        {
            /// <summary>
            /// Constructor
            /// </summary>
            internal XamlConstructorParameterTypeNode(
                int lineNumber,
                int linePosition,
                int depth,
                string   valueTypeFullName,  // String value of type, in the form Namespace.Typename
                string   valueAssemblyName,  // Assembly name where type value is defined.
                Type     valueElementType)   // Actual type of the valueTypeFullname.
                : base (XamlNodeType.ConstructorParameterType,lineNumber,linePosition,depth)
            {
                _valueTypeFullname = valueTypeFullName;
                _valueTypeAssemblyName = valueAssemblyName;
                _valueElementType = valueElementType;
            }

            /// <summary>
            /// Value for the parameter, which should resolve to a Type.
            /// This is in the form Namespace.LocalTypeName
            /// </summary>
            internal string ValueTypeFullName
            {
                get { return _valueTypeFullname;}
            }
            
            /// <summary>
            /// Name of the assembly where the resolved value's type is declared.
            /// </summary>
            internal string ValueTypeAssemblyName
            {
                get { return _valueTypeAssemblyName;}
            }
            
            /// <summary>
            /// Cached value of the type of the value that is resolved at compile time
            /// </summary>
            internal Type ValueElementType
            {
                get { return _valueElementType;}
            }

            string    _valueTypeFullname;
            string    _valueTypeAssemblyName;
            Type      _valueElementType;
        }
#endif

        /// <summary>
        /// Xaml End Element Node
        /// </summary>
        internal class XamlElementEndNode : XamlNode
        {
            /// <summary>
            /// Constructor
            /// </summary>
            internal XamlElementEndNode(
                int lineNumber,
                int linePosition,
                int depth)
                : this (XamlNodeType.ElementEnd,lineNumber,linePosition,depth)
            {
            }

            /// <summary>
            /// Constructor
            /// </summary>
            internal XamlElementEndNode(
                XamlNodeType tokenType,
                int          lineNumber,
                int          linePosition,
                int          depth)
                : base (tokenType,lineNumber,linePosition,depth)
            {
            }
        }

        /// <summary>
        /// XamlLiteralContentNode
        /// </summary>
        [DebuggerDisplay("Cont:{_content}")]
        internal class XamlLiteralContentNode : XamlNode
        {
            /// <summary>
            /// Constructor
            /// </summary>
            internal XamlLiteralContentNode(
                int    lineNumber,
                int    linePosition,
                int    depth,
                string content)
                : base (XamlNodeType.LiteralContent,lineNumber,linePosition,depth)
            {
                _content = content;
            }

            /// <summary>
            /// The Literal Content
            /// </summary>
            internal string Content
            {
                get { return _content; }
            }

            string _content;
        }


        /// <summary>
        /// XamlAttributeNode
        /// </summary>
        [DebuggerDisplay("Attr:{_value}")]
        internal class XamlAttributeNode : XamlNode
        {
            /// <summary>
            /// Constructor
            /// </summary>
            internal XamlAttributeNode(
                XamlNodeType tokenType,
                int          lineNumber,
                int          linePosition,
                int          depth,
                string       value)
                : base(tokenType,lineNumber,linePosition,depth)
            {
                _value = value;
            }

            /// <summary>
            /// Value for the attribute
            /// </summary>
            internal string Value
            {
                get { return _value;}
            }

            string _value;
        }
        
        /// <summary>
        /// Start of an Unknown element tag in Xaml.  This may be handled by
        /// a custom Serializer
        /// </summary>
       internal class XamlUnknownTagStartNode : XamlAttributeNode
        {
            /// <summary>
            /// Constructor
            /// </summary>
            internal XamlUnknownTagStartNode(
                int          lineNumber,
                int          linePosition,
                int          depth,
                string       xmlNamespace,
                string       value)
                : base(XamlNodeType.UnknownTagStart,lineNumber,linePosition,depth,
                       value)
            {
                _xmlNamespace = xmlNamespace;
            }
            
            /// <summary>
            /// XmlNamespace associated with the unknown tag
            /// </summary>
            internal string XmlNamespace
            {
                get { return _xmlNamespace;}
            }

            string _xmlNamespace;
        }
            
        /// <summary>
        /// End of an Unknown element tag in Xaml.  This may be handled by
        /// a custom Serializer
        /// </summary>
        internal class XamlUnknownTagEndNode : XamlNode
        {
            /// <summary>
            /// Constructor
            /// </summary>
            internal XamlUnknownTagEndNode(
                int    lineNumber,
                int    linePosition,
                int    depth,
                string localName,
                string xmlNamespace)
                : base (XamlNodeType.UnknownTagEnd,lineNumber,linePosition,depth)
            {
                _localName = localName;
                _xmlNamespace = xmlNamespace;
            }

#if PBTCOMPILER
            /// <summary>
            /// LocalName associated with the unknown tag
            /// </summary>
            internal string LocalName
            {
                get { return _localName; }
            }

            /// <summary>
            /// Xml Namespace associated with the unknown tag
            /// </summary>
            internal string XmlNamespace
            {
                get { return _xmlNamespace; }
            }

#endif  // PBTCOMPILER

            string _localName;
            string _xmlNamespace;
        }

#if !PBTCOMPILER
        /// <summary>
        /// XamlRoutedEventNode
        /// </summary>
        internal class XamlRoutedEventNode : XamlAttributeNode
        {
            /// <summary>
            /// Constructor
            /// </summary>
            internal XamlRoutedEventNode(
                int             lineNumber,
                int             linePosition,
                int             depth,
                RoutedEvent     routedEvent,
                string          assemblyName,
                string          typeFullName,
                string          routedEventName,
                string          value)
                : base (XamlNodeType.RoutedEvent,lineNumber,linePosition,depth,value)
            {
                _routedEvent = routedEvent;
                _assemblyName = assemblyName;
                _typeFullName =  typeFullName;
                _routedEventName =  routedEventName;
            }

            /// <summary>
            /// RoutedEvent ID for this node
            /// </summary>
            internal RoutedEvent Event
            {
                get { return _routedEvent;}
            }

            /// <summary>
            /// Assembly that contains the owner Type
            /// </summary>
            internal string AssemblyName
            {
                get { return _assemblyName; }
            }

            /// <summary>
            /// TypeFullName of the owner
            /// </summary>
            internal string TypeFullName
            {
                get { return _typeFullName; }
            }

            /// <summary>
            /// EventName
            /// </summary>
            internal string EventName
            {
                get { return _routedEventName;}
            }

            RoutedEvent    _routedEvent;
            string         _assemblyName;
            string         _typeFullName;
            string         _routedEventName;
        }

#endif

        /// <summary>
        /// XamlXmlnsPropertyNode
        /// </summary>
        [DebuggerDisplay("Xmlns:{_prefix)={_xmlNamespace}")]
        internal class XamlXmlnsPropertyNode : XamlNode
        {
            /// <summary>
            /// Constructor
            /// </summary>
            internal XamlXmlnsPropertyNode(
                int    lineNumber,
                int    linePosition,
                int    depth,
                string prefix,
                string xmlNamespace)
                : base (XamlNodeType.XmlnsProperty,lineNumber,linePosition,depth)
            {
                _prefix = prefix;
                _xmlNamespace = xmlNamespace;
            }

            /// <summary>
            /// Namespace Prefx
            /// </summary>
            internal string Prefix
            {
                get { return _prefix;}
            }

            /// <summary>
            /// XmlNamespace associated with the prefix
            /// </summary>
            internal string XmlNamespace
            {
                get { return _xmlNamespace;}
            }

            string _prefix;
            string _xmlNamespace;
        }


        /// <summary>
        /// XamlPIMappingNode which maps an xml namespace to a clr namespace and assembly
        /// </summary>
        [DebuggerDisplay("PIMap:{_xmlns}={_clrns};{_assy}")]
        internal class XamlPIMappingNode : XamlNode
        {
            /// <summary>
            /// Cosntructor
            /// </summary>
            internal XamlPIMappingNode(
                int    lineNumber,
                int    linePosition,
                int    depth,
                string xmlNamespace, 
                string clrNamespace, 
                string assemblyName)
                : base (XamlNodeType.PIMapping,lineNumber,linePosition,depth)
            {
                _xmlns = xmlNamespace;
                _clrns = clrNamespace;
                _assy = assemblyName;
            }

            /// <summary>
            /// Xml namespace for this mapping instruction
            /// </summary>
            internal string XmlNamespace
            {
                get { return _xmlns;}
            }

            /// <summary>
            /// Clr namespace that maps to the Xml namespace for this mapping instruction
            /// </summary>
            internal string ClrNamespace
            {
                get { return _clrns;}
            }
            
            /// <summary>
            /// Assembly where the CLR namespace object can be found
            /// </summary>
            internal string AssemblyName
            {
                get { return _assy;}
#if PBTCOMPILER
                set { _assy = value;}
#endif
            }

            string _xmlns;
            string _clrns;
            string _assy;
        }

      
        /// <summary>
        /// XamlClrEventNode, which is a clr event on any object.  Note that
        /// this may be on a DependencyObject, or any other object type.
        /// </summary>
        internal class XamlClrEventNode : XamlAttributeNode
        {
            /// <summary>
            /// Constructor
            /// </summary>
            internal XamlClrEventNode(
                int       lineNumber,
                int       linePosition,
                int       depth,
                string    eventName,
                MemberInfo eventMember, // Could either be an eventinfo or a methodinfo for the Add<EventName>Handler method
                string    value)
                : base (XamlNodeType.ClrEvent,lineNumber,linePosition,depth,value)
            {
#if PBTCOMPILER
                _eventName = eventName;
                _eventMember = eventMember;
#if HANDLEDEVENTSTOO
                _handledEventsToo = false;
#endif
#endif
            }

#if PBTCOMPILER
            /// <summary>
            /// Name of the Event
            /// </summary>
            internal string EventName
            {
                get { return _eventName;}
            }

            /// <summary>
            /// EventInfo for the for this event or
            /// MethodInfo for the Add{EventName}Handler method
            /// </summary>
            internal MemberInfo EventMember
            {
                get { return _eventMember; }
            }

            //
            // The type of the listener.  This is relevant for
            // attached events, where the type of the listener is
            // different from the type of the event.  This is used
            // by the markup compiler.
            //
            internal Type ListenerType
            {
                get { return _listenerType; }
                set { _listenerType = value; }
            }


#if HANDLEDEVENTSTOO
            /// <summary>
            /// HandledEventsToo flag for this event
            /// </summary>
            internal bool HandledEventsToo
            {
                get { return _handledEventsToo; }
                set { _handledEventsToo = value; }
            }
#endif
            /// <summary>
            /// This event is specified via an EventSetter in a Style.
            /// </summary>
            internal bool IsStyleSetterEvent
            {
                get { return _isStyleSetterEvent; }
                set { _isStyleSetterEvent = value; }
            }
            
            /// <summary>
            /// This event is an event attribute inside a template.
            /// </summary>
            internal bool IsTemplateEvent
            {
                get { return _isTemplateEvent; }
                set { _isTemplateEvent = value; }
            }
            
            /// <summary>
            /// An intermediary parser should skip processing this event if false
            /// </summary>
            internal bool IsOriginatingEvent
            {
                get { return _isOriginatingEvent; }
                set { _isOriginatingEvent = value; }
            }
            
            /// <summary>
            /// true if this event needs to be added to the current scope
            /// false if a new scope needs to be started and a new connectionId 
            ///       is generated
            /// </summary>
            internal bool IsSameScope
            {
                get { return _isSameScope; }
                set { _isSameScope = value; }
            }
            
            /// <summary>
            /// The short name of the local assembly if this event is locally defined
            /// </summary>
            internal string LocalAssemblyName
            {
                get { return _localAssemblyName; }
                set { _localAssemblyName = value; }
            }
            
            /// <summary>
            /// The connectionId identifying the scope to which this event belongs.
            /// </summary>
            internal Int32 ConnectionId
            {
                get { return _connectionId; }
                set { _connectionId = value; }
            }

#if HANDLEDEVENTSTOO
            bool _handledEventsToo;
#endif

            private Type _listenerType;
            bool _isStyleSetterEvent;
            bool _isTemplateEvent;
            bool _isOriginatingEvent = true;
            bool _isSameScope;
            string _localAssemblyName;
            Int32 _connectionId;
            string _eventName;
            MemberInfo _eventMember;
#endif
        }

        /// <summary>
        /// Xaml Array Start Property Node
        /// </summary>
        internal class XamlPropertyArrayStartNode : XamlPropertyComplexStartNode
        {
            /// <summary>
            /// Constructor
            /// </summary>
            internal XamlPropertyArrayStartNode(
                int          lineNumber,
                int          linePosition,
                int          depth,
                object       propertyMember,    // DependencyProperty, MethodInfo or PropertyInfo
                string       assemblyName,
                string       typeFullName,
                string       propertyName) : base (XamlNodeType.PropertyArrayStart,
                                                    lineNumber,
                                                    linePosition,
                                                    depth,
                                                    propertyMember,  
                                                    assemblyName,
                                                    typeFullName,
                                                    propertyName)
            {
            }
        }

        /// <summary>
        /// Xaml IList or IAddChild Start Property Node
        /// </summary>
        /// <remarks>
        /// This is meant for properties that have a list-like interface for adding items, and
        /// are read-only properties on an object.  The list-like interface may be IList,
        /// IEnumerator or IAddChild.  Note that if the property itself does not actually
        /// support IList or IAddChild, then the object that contains the property can implement
        /// IAddChild, and that will be used to add items.
        /// </remarks>
        internal class XamlPropertyIListStartNode : XamlPropertyComplexStartNode
        {
            /// <summary>
            /// Constructor
            /// </summary>
            internal XamlPropertyIListStartNode(
                int          lineNumber,
                int          linePosition,
                int          depth,
                object       propertyMember,    // DependencyProperty, MethodInfo or PropertyInfo
                string       assemblyName,
                string       typeFullName,
                string       propertyName) : base (XamlNodeType.PropertyIListStart,
                                                    lineNumber,
                                                    linePosition,
                                                    depth,
                                                    propertyMember,  
                                                    assemblyName,
                                                    typeFullName,
                                                    propertyName)
            {
            }
        }
        
        /// <summary>
        /// XamlIDictionaryPropertyNode
        /// </summary>
        internal class XamlPropertyIDictionaryStartNode : XamlPropertyComplexStartNode
        {
            /// <summary>
            /// Cosntructor
            /// </summary>
            internal XamlPropertyIDictionaryStartNode(
                int          lineNumber,
                int          linePosition,
                int          depth,
                object       propertyMember,    // DependencyProperty, MethodInfo or PropertyInfo
                string       assemblyName,
                string       typeFullName,
                string       propertyName) : base (XamlNodeType.PropertyIDictionaryStart,
                                                    lineNumber,
                                                    linePosition,
                                                    depth,
                                                    propertyMember,  
                                                    assemblyName,
                                                    typeFullName,
                                                    propertyName)
            {
            }
        }


        /// <summary>
        /// Xaml End Array Property Node
        /// </summary>
        internal class XamlPropertyArrayEndNode : XamlPropertyComplexEndNode
        {
            /// <summary>
            /// Constructor
            /// </summary>
            internal XamlPropertyArrayEndNode(
                int lineNumber,
                int linePosition,
                int depth)
                : base (XamlNodeType.PropertyArrayEnd,lineNumber,linePosition,depth)
            {
            }
        }
        
        /// <summary>
        /// Xaml End IList or IAddChild Property Node
        /// </summary>
        /// <remarks>
        /// This is meant for properties that have a list-like interface for adding items, and
        /// are read-only properties on an object.  The list-like interface may be IList,
        /// IEnumerator or IAddChild.  Note that if the property itself does not actually
        /// support IList or IAddChild, then the object that contains the property can implement
        /// IAddChild, and that will be used to add items.
        /// </remarks>
        internal class XamlPropertyIListEndNode : XamlPropertyComplexEndNode
        {
            /// <summary>
            /// Constructor
            /// </summary>
            internal XamlPropertyIListEndNode(
                int lineNumber,
                int linePosition,
                int depth)
                : base (XamlNodeType.PropertyIListEnd,lineNumber,linePosition,depth)
            {
            }
        }
        
        /// <summary>
        /// Xaml End IDictionary Property Node
        /// </summary>
        internal class XamlPropertyIDictionaryEndNode : XamlPropertyComplexEndNode
        {
            /// <summary>
            /// Constructor
            /// </summary>
            internal XamlPropertyIDictionaryEndNode(int lineNumber,int linePosition,int depth)
                : base (XamlNodeType.PropertyIDictionaryEnd,lineNumber,linePosition,depth)
            {
            }
        }



        /// <summary>
        /// Xaml End Attributes Node
        /// </summary>
        internal class XamlEndAttributesNode : XamlNode
        {
            /// <summary>
            /// Constructor
            /// </summary>
            internal XamlEndAttributesNode(
                int lineNumber,
                int linePosition,
                int depth, 
                bool compact)
                : base (XamlNodeType.EndAttributes,lineNumber,linePosition,depth)
            {
                _compact = compact;
            }

            /// <summary>
            /// True if this node ends the attributes of a compact attribute
            /// </summary>
            internal bool IsCompact { get { return _compact; } }

            bool _compact;
        }

        /// <summary>
        /// XamlDefTagNode
        /// </summary>
        internal class XamlDefTagNode : XamlAttributeNode
        {
            /// <summary>
            /// Constructor
            /// </summary>
            internal XamlDefTagNode(
                int       lineNumber,
                int       linePosition,
                int       depth,
                bool      isEmptyElement,
                XmlReader xmlReader,
                string    defTagName)
                : base (XamlNodeType.DefTag,lineNumber,linePosition,depth,defTagName)
            {
#if PBTCOMPILER
                _xmlReader = xmlReader;
                _isEmptyElement = isEmptyElement;
#endif                
            }

#if PBTCOMPILER
            /// <summary>
            /// XmlReader to use when reading the nodes
            /// </summary>
            internal XmlReader XmlReader
            {
                get { return _xmlReader; }
            }

            /// <summary>
            /// True if Reader is on an Empty element
            /// </summary>
            internal bool IsEmptyElement
            {
                get { return _isEmptyElement; }
            }

            XmlReader _xmlReader;
            bool _isEmptyElement;
#endif
        }


        /// <summary>
        /// XamlDefAttributeNode
        /// </summary>
       internal class XamlDefAttributeNode : XamlAttributeNode
        {
#if !PBTCOMPILER
            /// <summary>
            /// Constructor
            /// </summary>
            internal XamlDefAttributeNode(
                int    lineNumber,
                int    linePosition,
                int    depth,
                string name,
                string value)
                : base (XamlNodeType.DefAttribute,lineNumber,linePosition,depth,value)
            {
                _attributeUsage = BamlAttributeUsage.Default;
                _name = name;
            }
#endif

            /// <summary>
            /// Constructor
            /// </summary>
            internal XamlDefAttributeNode(
                int    lineNumber,
                int    linePosition,
                int    depth,
                string name,
                string value,
                BamlAttributeUsage bamlAttributeUsage)
                : base (XamlNodeType.DefAttribute,lineNumber,linePosition,depth,value)
            {
                _attributeUsage = bamlAttributeUsage;
                _name = name;
            }
            /// <summary>
            /// Name of the Attribute
            /// </summary>
            internal string Name
            {
                get { return _name; }
            }

            /// <summary>
            /// Gives the specific usage of this property
            /// </summary>
            /// <remarks>
            /// Some properties are not only set on an element, but have some other effects
            /// such as setting the xml:lang or xml:space values in the parser context.
            /// The AttributeUsage describes addition effects or usage for this property.
            /// </remarks>
            internal BamlAttributeUsage AttributeUsage
            {
                get { return _attributeUsage; }
            }

            BamlAttributeUsage _attributeUsage;
            string _name;
        }

        /// <summary>
        /// XamlDefAttributeNode
        /// </summary>
       internal class XamlDefAttributeKeyTypeNode : XamlAttributeNode
        {
            /// <summary>
            /// Constructor
            /// </summary>
            internal XamlDefAttributeKeyTypeNode(
                int    lineNumber,
                int    linePosition,
                int    depth,
                string value,         // Full type name of the type
                string assemblyName,  // Full assembly name where the value type is defined
                Type   valueType)     // Actual Type, if known.
                : base (XamlNodeType.DefKeyTypeAttribute,lineNumber,linePosition,depth,value)
            {
                _assemblyName = assemblyName;
                _valueType = valueType;
            }

            /// <summary>
            /// Name of the Assembly where the value type is defined.
            /// </summary>
            internal string AssemblyName
            {
                get { return _assemblyName; }
            }

            /// <summary>
            /// Gives the specific usage of this property
            /// </summary>
            internal Type ValueType
            {
                get { return _valueType; }
            }

            string  _assemblyName;
            Type    _valueType;
        }

        /// <summary>
        /// XamlPresentationOptionsAttributeNode
        /// </summary>
       internal class XamlPresentationOptionsAttributeNode : XamlAttributeNode
        {
            /// <summary>
            /// Constructor
            /// </summary>
            internal XamlPresentationOptionsAttributeNode(
                int    lineNumber,
                int    linePosition,
                int    depth,
                string name,
                string value)
                : base (XamlNodeType.PresentationOptionsAttribute,lineNumber,linePosition,depth,value)
            {
                _name = name;
            }
            
            /// <summary>
            /// Name of the Attribute
            /// </summary>
            internal string Name
            {
                get { return _name; }
            }

            /// <summary>
            /// Gives the specific usage of this property
            /// </summary>
            /// <remarks>
            /// Some properties are not only set on an element, but have some other effects
            /// such as setting the xml:lang or xml:space values in the parser context.
            /// The AttributeUsage describes addition effects or usage for this property.
            /// </remarks>
            //internal BamlAttributeUsage AttributeUsage
            //{
            //    get { return BamlAttributeUsage.Default; }
            //}

            string _name;
        }        

        /// <summary>
        /// Xaml start element Node that is used to define a section that is
        /// the key object for an IDictionary
        /// </summary>
        internal class XamlKeyElementStartNode : XamlElementStartNode
        {
            /// <summary>
            /// Constructor
            /// </summary>
            internal XamlKeyElementStartNode(
                int    lineNumber,
                int    linePosition,
                int    depth,
                string assemblyName,
                string typeFullName,
                Type   elementType,
                Type   serializerType)
                : base (XamlNodeType.KeyElementStart,
                        lineNumber,
                        linePosition,
                        depth,
                        assemblyName,
                        typeFullName,
                        elementType,
                        serializerType,
                        false,
                        false,
                        false)
            {
            }
        }

        /// <summary>
        /// XamlKeyElementEndNode
        /// </summary>
       internal class XamlKeyElementEndNode : XamlElementEndNode
        {
            /// <summary>
            /// Constructor
            /// </summary>
            internal XamlKeyElementEndNode(
                int    lineNumber,
                int    linePosition,
                int    depth)
                : base (XamlNodeType.KeyElementEnd,lineNumber,linePosition,depth)
            {
            }
        }


        internal class XamlContentPropertyNode : XamlNode
        {
            /// <summary>
            /// Constructor
            /// </summary>
            internal XamlContentPropertyNode(
                int         lineNumber,
                int         linePosition,
                int         depth,
                object      propertyMember,    // DependencyProperty or MethodInfo or PropertyInfo
                string      assemblyName,
                string      typeFullName,
                string      propertyName)
                : base (XamlNodeType.ContentProperty,
                        lineNumber,
                        linePosition,
                        depth)
            {
                if (typeFullName == null)
                {
                    throw new ArgumentNullException("typeFullName");
                }
                if (propertyName == null)
                {
                    throw new ArgumentNullException("propertyName");
                }
                
                _propertyMember = propertyMember;
                _assemblyName = assemblyName;
                _typeFullName = typeFullName;
                _propName = propertyName;
            }
            /// <summary>
            /// Assembly of the type that owns or has declared the Property
            /// </summary>
            internal string AssemblyName
            {
                get { return _assemblyName; }
            }

            /// <summary>
            /// TypeFullName of type that owns or has declared the Property
            /// </summary>
            internal string TypeFullName
            {
                get { return _typeFullName; }
            }

            /// <summary>
            /// Name of the Property
            /// </summary>
            internal string PropName
            {
                get { return _propName; }
            }

            /// <summary>
            /// Type of the owner or declarer of this Property
            /// </summary>
            internal Type PropDeclaringType
            {
                get
                {
                    // Lazy initialize this to avoid addition reflection if it
                    // is not needed.
                    if (_declaringType == null && _propertyMember != null)
                    {
                        _declaringType = XamlTypeMapper.GetDeclaringType(_propertyMember);
                    }
                    return _declaringType;
                }
            }

            /// <summary>
            /// Valid Type of the Property
            /// </summary>
            internal Type PropValidType
            {
                get
                {
                    // Lazy initialize this to avoid addition reflection if it
                    // is not needed.
                    if (_validType == null)
                    {
                        _validType = XamlTypeMapper.GetPropertyType(_propertyMember);
                    }
                    return _validType;
                }
            }

            Type   _declaringType;
            Type   _validType;
            object _propertyMember;
            string _assemblyName;
            string _typeFullName;
            string _propName;
        }
        #endregion  XamlNodeTypeDefitions
}
