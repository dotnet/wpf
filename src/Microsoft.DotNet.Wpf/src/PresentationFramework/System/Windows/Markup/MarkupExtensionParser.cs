// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Globalization;
using System.Security;
using MS.Utility;
using System.Collections.Specialized;
using System.Runtime.InteropServices;
using MS.Internal.Xaml.Parser;

#if PBTCOMPILER
namespace MS.Internal.Markup
#else
namespace System.Windows.Markup
#endif
{
    /// <summary>
    /// MarkupExtension parsing helper that provides things like namespace lookups
    /// and type resolution services.  This is implemented by classes like XamlReaderHelper
    /// and BamlWriter.
    /// </summary>
    internal interface IParserHelper
    {
        string LookupNamespace(string prefix);

        bool GetElementType(
                bool    extensionFirst,
                string  localName,
                string  namespaceURI,
            ref string  assemblyName,
            ref string  typeFullName,
            ref Type    baseType,
            ref Type    serializerType);

        bool CanResolveLocalAssemblies();
    }


    /// <summary>
    /// MarkupExtension parser class.
    /// </summary>
    internal class MarkupExtensionParser
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        internal MarkupExtensionParser(
            IParserHelper    parserHelper,
            ParserContext    parserContext)
        {
            _parserHelper = parserHelper;
            _parserContext = parserContext;
        }

        /// <summary>
        /// Return an intialized AttributeData structure if the attribute value adheres to the
        /// format for MarkupExtensions.  Otherwise return null.
        /// </summary>
        internal AttributeData IsMarkupExtensionAttribute(
                Type          declaringType,    // Type where propIdName is declared
                string        propIdName,       // Name of the property
            ref string        attrValue,
                int           lineNumber,
                int           linePosition,
                int           depth,
                object        info)             // PropertyInfo or DependencyProperty or MethodInfo for the property
        {
            string typeName;
            string args;

            if (!GetMarkupExtensionTypeAndArgs(ref attrValue, out typeName, out args))
            {
                return null;
            }

            return FillAttributeData(declaringType, propIdName, typeName, args,
                                     attrValue, lineNumber, linePosition, depth, info);
        }

        /// <summary>
        /// Return an intialized DefAttributeData structure if the attribute value adheres to the
        /// format for MarkupExtensions.  Otherwise return null.
        /// </summary>
        internal DefAttributeData IsMarkupExtensionDefAttribute(
                Type   declaringType,
            ref string attrValue,
                int    lineNumber,
                int    linePosition,
                int    depth)
        {
            string typeName;
            string args;

            if (!GetMarkupExtensionTypeAndArgs(ref attrValue, out typeName, out args))
            {
                return null;
            }

            return FillDefAttributeData(declaringType, typeName, args, attrValue,
                                        lineNumber, linePosition, depth);
        }

        /// <summary>
        ///  Applies some quick checks to see if an Attribute Value is a
        /// Markup Extension.  This is meant to be much cheaper than the full
        /// parse.   And this can return true even if the ME has some syntax errors.
        /// </summary>
        internal static bool LooksLikeAMarkupExtension(string attrValue)
        {
            if (attrValue.Length < 2)
                return false;
            if (attrValue[0] != '{')
                return false;
            if (attrValue[1] == '}')
                return false;

            return true;
        }

#if !PBTCOMPILER
        /// <summary>
        ///     Given a string that is to be treated as a literal string, check
        /// to see if it might be mistaken for a markup extension and escape it
        /// accordingly.
        /// </summary>
        /// <remarks>
        ///     Prefixing the string with "{}" will tell GetMarkupExtensionTypeAndArgs()
        /// that the remainder of the string is to be treated literally.
        /// </remarks>
        internal static string AddEscapeToLiteralString( string literalString )
        {
            string returnString = literalString;
            if (!String.IsNullOrEmpty(returnString) && returnString[0] == '{')
            {
                returnString = "{}" + returnString;
            }

            return returnString;
        }
#endif

        // Returns an known markup extension that can have a simple value. Also returns the property
        // name of the extension that can be used to set the value. This is used to strip it out of
        // the  args to get the simple value.
        private KnownElements GetKnownExtensionFromType(Type extensionType, out string propName)
        {
            if (KnownTypes.Types[(int)KnownElements.TypeExtension] == extensionType)
            {
                propName = "TypeName";
                return KnownElements.TypeExtension;
            }
            else if (KnownTypes.Types[(int)KnownElements.StaticExtension] == extensionType)
            {
                propName = "Member";
                return KnownElements.StaticExtension;
            }
            else if (KnownTypes.Types[(int)KnownElements.TemplateBindingExtension] == extensionType)
            {
                propName = "Property";
                return KnownElements.TemplateBindingExtension;
            }
            else if (KnownTypes.Types[(int)KnownElements.DynamicResourceExtension] == extensionType)
            {
                propName = "ResourceKey";
                return KnownElements.DynamicResourceExtension;
            }
            else if (KnownTypes.Types[(int)KnownElements.StaticResourceExtension] == extensionType)
            {
                propName = "ResourceKey";
                return KnownElements.StaticResourceExtension;
            }
            propName = string.Empty;
            return 0;
        }

        /// <summary>
        ///  Determine if the argument string passed in can represent a valid
        ///  type in the format
        ///     prefix:Classname or
        ///     TypeName = prefix:Classname
        ///  If so, then change the args string to contain only prefix:Classname and
        ///  return true.  Otherwise, return false.
        /// </summary>
        private bool IsSimpleTypeExtensionArgs(
                Type   extensionType,
                int    lineNumber,
                int    linePosition,
            ref string args)
        {
            if (KnownTypes.Types[(int)KnownElements.TypeExtension] == extensionType)
            {
                return IsSimpleExtensionArgs(lineNumber, linePosition, "TypeName", ref args, extensionType);
            }

            return false;
        }

        /// <summary>
        ///  Determine if the argument string passed in can represent a valid
        ///  param for a MarkuPExtension in one of these formats:
        ///     prefix:Classname
        ///     TypeName = prefix:Classname                           (TypeExtension)
        ///     prefix:Classname.MemberName
        ///     Member = prefix:Classname.MemberName                  (StaticExtension)
        ///     Property = prefix:Classname.MemberName                (TemplateBindingExtension)
        ///     {x:Type prefix:Classname}
        ///     {x:Static prefix:Classname.MemberName}
        ///     StringValue
        ///     ResourceKey = {x:Type prefix:Classname}               (DynamicResourceExtension)
        ///     ResourceKey = {x:Static prefix:Classname.MemberName}  (DynamicResourceExtension)
        ///     ResourceKey = StringValue                             (DynamicResourceExtension)
        ///  If so, then change the args string to contain only the raw value:
        ///     prefix:Classname  or
        ///     prefix:Classname.MemberName or
        ///     StringValue
        ///  and return true. Otherwise, return false.
        ///  isValueNestedExtension = true, if the args value is itself a StaticExtension or TypeExtension.
        ///  isValueTypeExtension = true, if the args value is itself a TypeExtension.
        ///  valueExtensions only apply to DynamicResourceExtension.
        /// </summary>
        private bool IsSimpleExtension(
                Type   extensionType,
                int    lineNumber,
                int    linePosition,
                int    depth,
            out short  extensionTypeId,
            out bool   isValueNestedExtension,
            out bool   isValueTypeExtension,
            ref string args)
        {
            bool isSimple = false;
            string propName;
            extensionTypeId = 0;
            isValueNestedExtension = false;
            isValueTypeExtension = false;

            // if we support optimizing for custom extensions, this can be generalized
            // to use a converter\serializer Id for that extension.
            KnownElements knownExtensionTypeId = GetKnownExtensionFromType(extensionType, out propName);

            if (knownExtensionTypeId != KnownElements.UnknownElement)
            {
                isSimple = IsSimpleExtensionArgs(lineNumber, linePosition, propName, ref args, extensionType);
            }

            if (isSimple)
            {
                switch (knownExtensionTypeId)
                {
                    case KnownElements.DynamicResourceExtension:
                    case KnownElements.StaticResourceExtension:

                        if (LooksLikeAMarkupExtension(args))
                        {
                            // if value may be a possible ME, see if it is a simple Type\StaticExtension.
                            // null is passed for propIdName to indicate this.
                            AttributeData nestedAttrData = IsMarkupExtensionAttribute(extensionType,
                                                                                      null,
                                                                                  ref args,
                                                                                      lineNumber,
                                                                                      linePosition,
                                                                                      depth,
                                                                                      null);

                            isValueTypeExtension = nestedAttrData.IsTypeExtension;
                            isSimple = isValueTypeExtension || nestedAttrData.IsStaticExtension;
                            isValueNestedExtension = isSimple;
                            if (isSimple)
                            {
                                // if nested extension value is simple, take the simple args
                                args = nestedAttrData.Args;
                            }
                            else
                            {
                                // else restore the original args for normal processing
                                args += "}";
                            }
                        }

                        break;
                }

                if (isSimple)
                {
                    extensionTypeId = (short)knownExtensionTypeId;
                }
            }

            return isSimple;
        }

        private bool IsSimpleExtensionArgs(int lineNumber,
                                           int linePosition,
                                           string propName,
                                       ref string args,
                                       Type targetType)
        {
            // We have a MarkupExtension, so process the argument string to determine
            // if it is simple.  Do this by tokenizing now and extracting the simple
            // type string.
            ArrayList tokens = TokenizeAttributes(args, lineNumber, linePosition, targetType);
            if (tokens == null)
            {
                return false;
            }

            if (tokens.Count == 1)
            {
                args = (String)tokens[0];
                return true;
            }

            if (tokens.Count == 3 &&
                (string)tokens[0] == propName)
            {
                args = (String)tokens[2];
                return true;
            }

            return false;
        }

        /// <summary>
        /// Parse the attrValue string into a typename and arguments.  Return true if
        /// they parse successfully.
        /// </summary>
        /// <remarks>
        /// Localization API also relys on this method to filter markup extensions, as they are not
        /// localizable by default.
        /// </remarks>
        internal static bool GetMarkupExtensionTypeAndArgs(
            ref string attrValue,
            out string typeName,
            out string args)
        {
            int length = attrValue.Length;
            typeName = string.Empty;
            args = string.Empty;

            // MarkupExtensions MUST have '{' as the first character
            if (length < 1 || attrValue[0] != '{')
            {
                return false;
            }

            bool gotEscape = false;
            StringBuilder stringBuilder = null;
            int i = 1;

            for (; i<length; i++)
            {
                // Skip all whitespace unless we are collecting characters for the
                // type name.
                if (Char.IsWhiteSpace(attrValue[i]))
                {
                    if (stringBuilder != null)
                    {
                        break;
                    }
                }
                else
                {
                    // If there is no string builder, then we haven't encountered the
                    // first non-whitespace character after '{'.
                    if (stringBuilder == null)
                    {
                        // Always escape the first '\'
                        if (!gotEscape && attrValue[i] == '\\')
                        {
                            gotEscape = true;
                        }
                        // We have the first non-whitespace character after '{'
                        else if (attrValue[i] == '}')
                        {
                            // Found the closing '}', so we're done.  If this is the
                            // second character, we have an empty MarkupExtension, which
                            // is a way to escape a string.  In that case, trim off
                            // the first two characters from attrValue so that the caller
                            // will get the unescaped string.
                            if (i == 1)
                            {
                                attrValue = attrValue.Substring(2);
                                return false;
                            }
                        }
                        else
                        {
                            stringBuilder = new StringBuilder(length - i);
                            stringBuilder.Append(attrValue[i]);
                            gotEscape = false;
                        }
                    }
                    else
                    {
                        // Always escape the first '\'
                        if (!gotEscape && attrValue[i] == '\\')
                        {
                            gotEscape = true;
                        }
                        else if (attrValue[i] == '}')
                        {
                            // Found the closing '}', so we're done.
                            break;
                        }
                        else
                        {
                            // Collect characters that make up the type name
                            stringBuilder.Append(attrValue[i]);
                            gotEscape = false;
                        }
                    }
                }
            }

            // Set typeName and arguments.  Note that both may be empty, but having
            // an empyt typeName will generate an error later on.
            if (stringBuilder != null)
            {
                typeName = stringBuilder.ToString();
            }

            if (i < length-1)
            {
                args = attrValue.Substring(i, length-i);
            }
            else if( attrValue[length-1] == '}')
            {
                args = "}";
            }

            return true;
        }

        /// <summary>
        /// Fill the def attribute data structure with type and attribute string information.
        /// Note that this is not for general def attributes, but only for the key attribute
        /// used when storing items in an IDictionary.
        /// </summary>
        private DefAttributeData FillDefAttributeData(
                Type   declaringType,    // Type where attribute is declared
                string typename,
                string args,
                string attributeValue,
                int    lineNumber,
                int    linePosition,
                int    depth)
        {
            string namespaceURI;
            string targetAssemblyName;
            string targetFullName;
            Type targetType;
            Type serializerType;
            bool isSimple = false;

            bool resolvedTag = GetExtensionType(typename, attributeValue, lineNumber, linePosition,
                                                out namespaceURI, out targetAssemblyName,
                                                out targetFullName, out targetType, out serializerType);

            if (resolvedTag)
            {
                isSimple = IsSimpleTypeExtensionArgs(targetType,
                                                     lineNumber,
                                                     linePosition,
                                                     ref args);
            }

            return new DefAttributeData(targetAssemblyName, targetFullName,
                        targetType, args, declaringType, namespaceURI,
                        lineNumber, linePosition, depth, isSimple);
        }

        // Fill the attribute data structure with type and attribute string information
        private AttributeData FillAttributeData(
                Type   declaringType,    // Type where propIdName is declared
                string propIdName,       // Name of the property
                string typename,
                string args,
                string attributeValue,
                int    lineNumber,
                int    linePosition,
                int    depth,
                object info)            // PropertyInfo or DependencyProperty or MethodInfo for the property
        {
            string namespaceURI;
            string targetAssemblyName;
            string targetFullName;
            Type targetType;
            Type serializerType;
            bool isSimple = false;
            short extensionId = 0;
            bool isValueNestedExtension = false;
            bool isValueTypeExtension = false;

            bool resolvedTag = GetExtensionType(typename, attributeValue, lineNumber, linePosition,
                                                out namespaceURI, out targetAssemblyName,
                                                out targetFullName, out targetType, out serializerType);

            // propIdName is an empty string only for the case when args is a ctor param of a MarkupExtension
            if (resolvedTag && propIdName != string.Empty)
            {
                if (propIdName == null)
                {
                    // If propIdName is null, then we are looking for nested simple extensions and
                    // we allow only Type\StaticExtension.
                    if (KnownTypes.Types[(int)KnownElements.TypeExtension] == targetType)
                    {
                        isSimple = IsSimpleExtensionArgs(lineNumber, linePosition, "TypeName", ref args, targetType);
                        isValueNestedExtension = isSimple;
                        isValueTypeExtension = isSimple;
                        extensionId = (short)KnownElements.TypeExtension;
                    }
                    else if (KnownTypes.Types[(int)KnownElements.StaticExtension] == targetType)
                    {
                        isSimple = IsSimpleExtensionArgs(lineNumber, linePosition, "Member", ref args, targetType);
                        isValueNestedExtension = isSimple;
                        extensionId = (short)KnownElements.StaticExtension;
                    }
                }
                else
                {
                    propIdName = propIdName.Trim();

                    isSimple = IsSimpleExtension(targetType,
                                                 lineNumber,
                                                 linePosition,
                                                 depth,
                                                 out extensionId,
                                                 out isValueNestedExtension,
                                                 out isValueTypeExtension,
                                                 ref args);
                }
            }

            return new AttributeData(targetAssemblyName, targetFullName,
                        targetType, args, declaringType, propIdName, info,
                        serializerType, lineNumber, linePosition, depth, namespaceURI,
                        extensionId, isValueNestedExtension, isValueTypeExtension, isSimple);
        }

        private bool GetExtensionType(
                    string typename,
                    string attributeValue,
                    int    lineNumber,
                    int    linePosition,
                out string namespaceURI,
                out string targetAssemblyName,
                out string targetFullName,
                out Type   targetType,
                out Type   serializerType)
        {
            targetAssemblyName = null;
            targetFullName     = null;
            targetType         = null;
            serializerType     = null;

            // lookup the type of the target
            string fullname = typename;
            string prefix = String.Empty;
            int typeIndex = typename.IndexOf(':');
            if (typeIndex >= 0)
            {
                prefix = typename.Substring(0, typeIndex);
                typename = typename.Substring(typeIndex + 1);
            }

            namespaceURI = _parserHelper.LookupNamespace(prefix);

            bool resolvedTag = _parserHelper.GetElementType(true, typename, namespaceURI,
                        ref targetAssemblyName, ref targetFullName, ref targetType, ref serializerType);

            if (!resolvedTag)
            {
                if (_parserHelper.CanResolveLocalAssemblies())
                {
                    // if local assemblies can be resolved, but the type could not be resolved, then
                    // we need to throw an exception
                    ThrowException(SRID.ParserNotMarkupExtension, attributeValue, typename,
                                   namespaceURI, lineNumber, linePosition);
                }
                else
                {
                    // if local assemblies cannot yet be resolved, we record the data that we will need
                    // to write an unknown tag start, and note in the data that the type is an unknown
                    // markup extension.
                    targetFullName = fullname;
                    targetType = typeof(UnknownMarkupExtension);
                }
            }
            else if (!KnownTypes.Types[(int)KnownElements.MarkupExtension].IsAssignableFrom(targetType))
            {
                // if the type is not known, throw an exception
                ThrowException(SRID.ParserNotMarkupExtension, attributeValue, typename,
                               namespaceURI, lineNumber, linePosition);
            }

            return resolvedTag;
        }

        /// <summary>
        /// Fill the attribute data structure with type and attribute string information.
        /// Note that this is for general properties and not def attributes.
        /// </summary>
        internal ArrayList CompileAttributes(
            ArrayList markupExtensionList,
            int       startingDepth)
        {
            ArrayList xamlNodes = new ArrayList(markupExtensionList.Count * 5);

            for (int i = 0; i<markupExtensionList.Count; i++)
            {
                AttributeData data = (AttributeData)markupExtensionList[i];

                CompileAttribute(xamlNodes, data);
            }
            return xamlNodes;
        }

        /// <summary>
        /// Create nodes for a complex property that surrounds an element tree.
        /// Note that this is for general properties and not def attributes.
        /// </summary>
        internal void CompileAttribute(
            ArrayList xamlNodes,
            AttributeData data)
        {
            // For MarkupExtension syntax, treat PropertyInfo as a CLR property, but MethodInfo
            // and DependencyProperty as a DependencyProperty.  Note that the MarkupCompiler
            // will handle the case where a DependencyProperty callback is made if it gets
            // a MethodInfo for the attached property setter.
            string declaringTypeAssemblyName = data.DeclaringType.Assembly.FullName;
            string declaringTypeFullName = data.DeclaringType.FullName;

            // Find the PropertyRecordType to use in this case

            Type propertyType;
            bool propertyCanWrite;
            XamlTypeMapper.GetPropertyType(data.Info, out propertyType, out propertyCanWrite);
            BamlRecordType propertyRecordType = BamlRecordManager.GetPropertyStartRecordType(propertyType, propertyCanWrite);

            // Create the property start and end records

            XamlNode propertyStart;
            XamlNode propertyEnd;

            switch (propertyRecordType)
            {
                case BamlRecordType.PropertyArrayStart:
                {
                    propertyStart = new XamlPropertyArrayStartNode(
                                                                data.LineNumber,
                                                                data.LinePosition,
                                                                data.Depth,
                                                                data.Info,
                                                                declaringTypeAssemblyName,
                                                                declaringTypeFullName,
                                                                data.PropertyName);

                    propertyEnd = new XamlPropertyArrayEndNode(
                                          data.LineNumber,
                                          data.LinePosition,
                                          data.Depth);
                    break;
                }
                case BamlRecordType.PropertyIDictionaryStart:
                {
                    propertyStart = new XamlPropertyIDictionaryStartNode(
                                                                data.LineNumber,
                                                                data.LinePosition,
                                                                data.Depth,
                                                                data.Info,
                                                                declaringTypeAssemblyName,
                                                                declaringTypeFullName,
                                                                data.PropertyName);
                    propertyEnd = new XamlPropertyIDictionaryEndNode(
                                          data.LineNumber,
                                          data.LinePosition,
                                          data.Depth);
                    break;
                }
                case BamlRecordType.PropertyIListStart:
                {
                    propertyStart = new XamlPropertyIListStartNode(
                                                                data.LineNumber,
                                                                data.LinePosition,
                                                                data.Depth,
                                                                data.Info,
                                                                declaringTypeAssemblyName,
                                                                declaringTypeFullName,
                                                                data.PropertyName);
                    propertyEnd = new XamlPropertyIListEndNode(
                                          data.LineNumber,
                                          data.LinePosition,
                                          data.Depth);
                    break;
                }
                default: // PropertyComplexStart
                {
                    propertyStart = new XamlPropertyComplexStartNode(
                                                                data.LineNumber,
                                                                data.LinePosition,
                                                                data.Depth,
                                                                data.Info,
                                                                declaringTypeAssemblyName,
                                                                declaringTypeFullName,
                                                                data.PropertyName);
                    propertyEnd = new XamlPropertyComplexEndNode(
                                          data.LineNumber,
                                          data.LinePosition,
                                          data.Depth);
                    break;
                }
            }

            // NOTE:  Add duplicate property checking here as is done in XamlReaderHelper
            xamlNodes.Add(propertyStart);

            CompileAttributeCore(xamlNodes, data);

            xamlNodes.Add(propertyEnd);
        }

        /// <summary>
        /// Create nodes for an element tree.
        /// Note that this is for general properties and not def attributes.
        /// </summary>
        internal void CompileAttributeCore(
            ArrayList xamlNodes,
            AttributeData data)
        {
            string typename = null;
            string namespaceURI = null;
            ArrayList list = TokenizeAttributes(data.Args, data.LineNumber, data.LinePosition, data.TargetType);

            // If the list is empty, or the second item on the list is an equal sign, then
            // we have a simple markup extension that uses the default constructor.  In all
            // other cases we must have at least one constructor parameter.

            if (data.TargetType == typeof(UnknownMarkupExtension))
            {
                // If the target type is unknown, then we record an unknown tag start, rather
                // than an element start.
                typename = data.TargetFullName;
                string prefix = String.Empty;
                int typeIndex = typename.IndexOf(':');
                if (typeIndex >= 0)
                {
                    prefix = typename.Substring(0, typeIndex);
                    typename = typename.Substring(typeIndex + 1);
                }

                namespaceURI = _parserHelper.LookupNamespace(prefix);

                xamlNodes.Add(new XamlUnknownTagStartNode(
                                  data.LineNumber,
                                  data.LinePosition,
                                  ++data.Depth,
                                  namespaceURI,
                                  typename));
            }
            else
            {
                xamlNodes.Add(new XamlElementStartNode(
                                  data.LineNumber,
                                  data.LinePosition,
                                  ++data.Depth,
                                  data.TargetAssemblyName,
                                  data.TargetFullName,
                                  data.TargetType,
                                  data.SerializerType));
            }

            xamlNodes.Add(new XamlEndAttributesNode(
                                  data.LineNumber,
                                  data.LinePosition,
                                  data.Depth,
                                  true));
            int listIndex = 0;
            if (list != null &&
                (list.Count == 1 ||
                 (list.Count > 1 && !(list[1] is String) && ((Char)list[1] == ','))))
            {
                // Go through the constructor parameters, writing them out like complex
                // properties
                WriteConstructorParams(xamlNodes, list, data, ref listIndex);
            }

            // Write properties that come after the element constructor parameters
            WriteProperties(xamlNodes, list, listIndex, data);

            // close up
            if (data.TargetType == typeof(UnknownMarkupExtension))
            {
                xamlNodes.Add(new XamlUnknownTagEndNode(
                                   data.LineNumber,
                                   data.LinePosition,
                                   data.Depth--,
                                   typename,
                                   namespaceURI));
            }
            else
            {
                xamlNodes.Add(new XamlElementEndNode(
                                   data.LineNumber,
                                   data.LinePosition,
                                   data.Depth--));
            }
        }

        /// <summary>
        /// Parse the string representation of a set of def attributes in MarkupExtension
        /// syntax and return a list of xaml nodes that represents those attributes.
        /// </summary>
        internal ArrayList CompileDictionaryKeys(
            ArrayList   complexDefAttributesList,
            int         startingDepth)
        {
            ArrayList xamlNodes = new ArrayList(complexDefAttributesList.Count * 5);
            for (int i = 0; i<complexDefAttributesList.Count; i++)
            {
                DefAttributeData data = (DefAttributeData)complexDefAttributesList[i];

                CompileDictionaryKey(xamlNodes, data);
            }
            return xamlNodes;
        }

        /// <summary>
        /// Parse the string representation of a set of def attributes in MarkupExtension
        /// syntax and return a list of xaml nodes that represents those attributes.
        /// </summary>
        internal void CompileDictionaryKey(
            ArrayList   xamlNodes,
            DefAttributeData data)
        {
            ArrayList list = TokenizeAttributes(data.Args, data.LineNumber, data.LinePosition, data.TargetType);

            // If the list is empty, or the second item on the list is an equal sign, then
            // we have a simple markup extension that uses the default constructor.  In all
            // other cases we must have at least one constructor parameter.
            xamlNodes.Add(new XamlKeyElementStartNode(
                                  data.LineNumber,
                                  data.LinePosition,
                                  ++data.Depth,
                                  data.TargetAssemblyName,
                                  data.TargetFullName,
                                  data.TargetType,
                                  null));
            xamlNodes.Add(new XamlEndAttributesNode(
                                  data.LineNumber,
                                  data.LinePosition,
                                  data.Depth,
                                  true));
            int listIndex = 0;
            if (list != null &&
                (list.Count == 1 ||
                 (list.Count > 1 && !(list[1] is String) && ((Char)list[1] == ','))))
            {
                // Go through the constructor parameters, writing them out like complex
                // properties
                WriteConstructorParams(xamlNodes, list, data, ref listIndex);
            }

            // Write properties that come after the element constructor parameters
            WriteProperties(xamlNodes, list, listIndex, data);

            // close up
            xamlNodes.Add(new XamlKeyElementEndNode(
                               data.LineNumber,
                               data.LinePosition,
                               data.Depth--));
        }

        /// <summary>
        /// The core method that writes out the MarkupExtension itself without the surrounding
        /// contextual xaml nodes.
        /// The format of compact syntax is
        ///     "{typename constParam1, constParam2, name = value, name = value,  ... }"
        /// (whitespace is ignored near delimiters).  The effect of this is to
        /// create a new object of the given type, using the provided constructor
        /// parameters.  If they are absent, use the default constructor.  Then to set
        /// its properties. Each name=value pair causes a property
        /// with the given name to be set to the given value (after type conversion).
        ///
        /// For constructor parameters, or on right-hand side of the = sign, some
        /// characters are treated specially:
        ///     \     - escape charater - quotes the following character (including \)
        ///     ,     - terminates the clause
        ///     {}  - matching braces, meaning a nested MarkupExtension
        ///     ''    - matching single quotes
        ///     ""    - matching double quotes
        /// Inside matching delimiters, comma has no special meaning and
        /// delimiters must nest correctly (unless escaped).  Inside matching
        /// quotes (either kind), no characters are special except \.
        ///
        /// If the string really is in "MarkupExtension syntax" form, return true.
        /// 
        ///  Exceptions are thrown for mismatching delimiters, and for errors while
        /// assigning to properties.
        /// Major changes in 4.6.2 :
        /// MarkupExtensionBracketCharacterAttributes : Specified on a particular markup extension property,
        /// these can be a pair of special characters (like (), [] etc). Anything enclosed inside these characters
        /// has no special meaning, except \ and other such MarkupExtensionBracketCharacters themselves.
        ///  </summary>
        private ArrayList TokenizeAttributes (
            string args,
            int    lineNumber,
            int    linePosition,
            Type extensionType)
        {
            // As a result of having to rely on Reflection to find whether a property has a MarkupExtensionBracketCharacterAttribute
            // set on it, we can't parse a locally defined Markup Extension in MarkupCompilePass1. Therefore, if we find an UnknownExtension
            // here, we just return null. If this was MarkupCompilePass2 and the extension was still unknown, it would have errored out by now
            // already.

            if (extensionType == typeof (UnknownMarkupExtension))
            {
                return null;
            }

            int maxConstructorParams = 0;
            ParameterInfo[] constructorParameters = FindLongestConstructor(extensionType, out maxConstructorParams);

            Dictionary<string, SpecialBracketCharacters> bracketCharacterCache = _parserContext.InitBracketCharacterCacheForType(extensionType);
            Stack<char> bracketCharacterStack = new Stack<char>();
            int currentConstructorParam = 0;
            bool inCtorParsingMode = constructorParameters != null && maxConstructorParams > 0;
            bool inBracketCharacterMode = false;
            
            ArrayList list = null;
            int length = args.Length;
            bool inQuotes = false;
            bool gotEscape = false;
            bool nonWhitespaceFound = false;
            bool gotFinalCloseCurly = false;
            Char quoteChar = '\'';
            int  leftCurlies = 0;
            StringBuilder stringBuilder = null;
            int i = 0;
            string parameterName = null;
            SpecialBracketCharacters bracketCharacters = null;
            if (inCtorParsingMode && bracketCharacterCache != null)
            {
                parameterName = maxConstructorParams > 0 ? constructorParameters[currentConstructorParam].Name : null;
                if (!string.IsNullOrEmpty(parameterName))
                {
                    bracketCharacters = GetBracketCharacterForProperty(parameterName, bracketCharacterCache);
                }
            }

            // Loop through the args, creating a list of arguments and known delimiters.
            // This loop does limited syntax checking, and serves to tokenize the argument
            // string into chunks that are validated in greater detail in the next phase.
            for (i=0; i < length && !gotFinalCloseCurly; i++)
            {
                // Escape character is always in effect for everything inside
                // a MarkupExtension.  We have to remember that the next character is
                // escaped, and is not treated as a quote or delimiter.
                if (!gotEscape && args[i] == '\\')
                {
                    gotEscape = true;
                    continue;
                }

                if (!nonWhitespaceFound && !Char.IsWhiteSpace(args[i]))
                {
                    nonWhitespaceFound = true;
                }

                // Process all characters that are not whitespace or are between quotes
                if (inQuotes || leftCurlies > 0 || nonWhitespaceFound)
                {
                    // We have a non-whitespace character, so ensure we have
                    // a string builder to accumulate characters and a list to collect
                    // attributes and delimiters.  These are lazily
                    // created so that simple cases that have no parameters do not
                    // create any extra objects.
                    if (stringBuilder == null)
                    {
                        stringBuilder = new StringBuilder(length);
                        list = new ArrayList(1);
                    }

                    // If the character is escaped, then it is part of the attribute
                    // being collected, regardless of its value and is not treated as
                    // a delimiter or special character.  Write back the escape
                    // character since downstream processing will need it to determine
                    // whether the value is a MarkupExtension or not, and to prevent
                    // multiple escapes from being lost by recursive processing.
                    if (gotEscape)
                    {
                        stringBuilder.Append('\\');
                        stringBuilder.Append(args[i]);
                        gotEscape = false;
                        continue;
                    }

                    // If quoted or inside curlies then scoop up everything.
                    // Track yet deeper nestings of curlies.
                    if (inQuotes || leftCurlies > 0)
                    {
                        if (inQuotes && args[i] == quoteChar)
                        {
                            // If we're inside quotes, then only an end quote that is not
                            // escaped is special, and will act as a delimiter.
                            inQuotes = false;
                            nonWhitespaceFound = false;

                            // Don't trim leading and trailing spaces that were inside quotes.
                            AddToTokenList(list, stringBuilder, false);
                        }
                        else
                        {
                            if (leftCurlies > 0 && args[i] == '}')
                            {
                                leftCurlies--;
                            }
                            else if (args[i] == '{')
                            {
                                leftCurlies++;
                            }
                            stringBuilder.Append(args[i]);
                        }
                    }
                    // If we are inside of MarkupExtensionBracketCharacters for a particular property or position parameter,
                    // scoop up everything inside one by one, and keep track of nested Bracket Characters in the stack. 
                    else if (inBracketCharacterMode)
                    {
                        stringBuilder.Append(args[i]);
                        if (bracketCharacters.StartsEscapeSequence(args[i]))
                        {
                            bracketCharacterStack.Push(args[i]);
                        }
                        else if (bracketCharacters.EndsEscapeSequence(args[i]))
                        {
                            if (bracketCharacters.Match(bracketCharacterStack.Peek(), args[i]))
                            {
                                bracketCharacterStack.Pop();
                            }
                            else
                            {
                                ThrowException(SRID.ParserMarkupExtensionInvalidClosingBracketCharacers, args[i].ToString(), lineNumber, linePosition);
                            }
                        }

                        if (bracketCharacterStack.Count == 0)
                        {
                            inBracketCharacterMode = false;
                        }
                    }
                    else  // not in quotes or inside nested curlies.  Parse the Tokens
                    { // not in special escape mode either
                        switch(args[i])
                        {
                        case '"':
                        case '\'':
                            // If we're not inside quotes, then a start quote can only
                            // occur as the first non-whitespace character in a name or value.
                            if (stringBuilder.Length != 0)
                            {
                                ThrowException(SRID.ParserMarkupExtensionNoQuotesInName, args,
                                               lineNumber, linePosition);
                            }
                            inQuotes = true;
                            quoteChar = args[i];
                            break;

                        case ',':
                        case '=':
                            if (inCtorParsingMode && args[i] == ',')
                            {
                                inCtorParsingMode = ++currentConstructorParam < maxConstructorParams;
                                if (inCtorParsingMode)
                                {
                                    parameterName = constructorParameters[currentConstructorParam].Name;
                                    bracketCharacters = GetBracketCharacterForProperty(parameterName, bracketCharacterCache);
                                }
                            }

                            // If we have a token in the stringbuilder, then store it
                            if (stringBuilder != null && stringBuilder.Length > 0)
                            {
                                AddToTokenList(list, stringBuilder, true);
                                if (bracketCharacterStack.Count != 0)
                                {
                                    ThrowException(SRID.ParserMarkupExtensionMalformedBracketCharacers, bracketCharacterStack.Peek().ToString(), lineNumber, linePosition);
                                }
                            }
                            else if (list.Count == 0)
                            {
                                // Must have an attribute before you have the first delimeter
                                ThrowException(SRID.ParserMarkupExtensionDelimiterBeforeFirstAttribute, args,
                                               lineNumber, linePosition);
                            }
                            else if (list[list.Count - 1] is Char)
                            {
                                // Can't have two delimiters in a row, so check what is on
                                // the list and complain if the last item is a character, or if
                                // a delimiter is the first item.
                                ThrowException(SRID.ParserMarkupExtensionBadDelimiter, args,
                                               lineNumber, linePosition);
                            }

                            if (args[i] == '=')
                            {
                                inCtorParsingMode = false;
                                // find BracketCharacterAttribute for the property name that preceeded this = delimiter
                                parameterName = (string) list[list.Count - 1];
                                bracketCharacters = GetBracketCharacterForProperty(parameterName, bracketCharacterCache);
                            }

                            // Append known delimiters.
                            list.Add(args[i]);
                            nonWhitespaceFound = false;
                            break;

                        case '}':
                            // If we hit the outside right curly brace, then end processing.  If
                            // there is a delimiter on the top of the stack and we haven't
                            // hit another non-whitespace character, then its an error
                            gotFinalCloseCurly = true;
                            if (stringBuilder != null)
                            {
                                if (stringBuilder.Length > 0)
                                {
                                    AddToTokenList(list, stringBuilder, true);
                                }
                                else if (list.Count > 0 && (list[list.Count-1] is Char))
                                {
                                    ThrowException(SRID.ParserMarkupExtensionBadDelimiter, args,
                                                   lineNumber, linePosition);
                                }
                            }
                            break;

                        case '{':
                            leftCurlies++;
                            stringBuilder.Append(args[i]);
                            break;

                        default:
                            if (bracketCharacters != null && bracketCharacters.StartsEscapeSequence(args[i]))
                            {
                                bracketCharacterStack.Clear();
                                bracketCharacterStack.Push(args[i]);
                                inBracketCharacterMode = true;
                            }

                            // Must just be a plain old character, so add it to the stringbuilder
                            stringBuilder.Append(args[i]);
                            break;
                        }
                    }
                }
            }


            // If we've accumulated content but haven't hit a terminating '}' then the
            // format is bad, so complain.
            if (!gotFinalCloseCurly)
            {
                ThrowException(SRID.ParserMarkupExtensionNoEndCurlie, "}", lineNumber, linePosition);
            }
            // If there is junk after the closing '}', complain
            else if (i < length)
            {
                ThrowException(SRID.ParserMarkupExtensionTrailingGarbage, "}",
                               args.Substring(i,length-(i)), lineNumber, linePosition);
            }

            return list;
        }


        private static void AddToTokenList(ArrayList list, StringBuilder sb, bool trim)
        {
            if(trim)
            {
                Debug.Assert(sb.Length > 0);
                Debug.Assert(!Char.IsWhiteSpace(sb[0]));

                int i = sb.Length-1;
                while(Char.IsWhiteSpace(sb[i]))
                    --i;
                sb.Length = i+1;
            }
            list.Add(sb.ToString());
            sb.Length = 0;
        }

        /// <summary>
        /// Returns the list of parameters of the constructor with the most number
        /// of arguments.
        /// </summary>
        private ParameterInfo[] FindLongestConstructor(Type extensionType, out int maxConstructorArguments)
        {
            ParameterInfo[] constructorParameters = null;
            ConstructorInfo[] constructors = extensionType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            maxConstructorArguments = 0;
            foreach (ConstructorInfo ctor in constructors)
            {
                ParameterInfo[] parInfo = ctor.GetParameters();
                if (parInfo.Length >= maxConstructorArguments)
                {
                    maxConstructorArguments = parInfo.Length;
                    constructorParameters = parInfo;
                }
            }

            return constructorParameters;
        }

        /// <summary>
        /// At this point the start element is written, and we have to process all
        /// the constructor parameters that follow.  Stop when we hit the first
        /// name=value, or when the end of the attributes is reached.
        /// </summary>
        private void WriteConstructorParams(
                ArrayList        xamlNodes,
                ArrayList        list,
                DefAttributeData data,
            ref int              listIndex)
        {
#if PBTCOMPILER
            int numberOfConstructorAttributes = 0;
#endif

            if (list != null && listIndex < list.Count)
            {
                // Mark the start of the constructor parameter section.  Nodes directly
                // under this one are constructor parameters.  Note that we can have
                // element trees under here.
                xamlNodes.Add(new XamlConstructorParametersStartNode(
                                        data.LineNumber,
                                        data.LinePosition,
                                        ++data.Depth));

                for (; listIndex < list.Count; listIndex+=2)
                {
                    if (!(list[listIndex] is String))
                    {
                        ThrowException(SRID.ParserMarkupExtensionBadConstructorParam, data.Args,
                                       data.LineNumber, data.LinePosition);
                    }

                    // If the next item after the current one is '=', then we've hit the
                    // start of named parameters, so stop
                    if (list.Count > (listIndex+1) &&
                        list[listIndex+1] is Char &&
                        (Char)list[listIndex+1] == '=')
                    {
                        break;
                    }

#if PBTCOMPILER
                    numberOfConstructorAttributes++;
#endif

                    // Handle nested markup extensions by recursing here.  If the
                    // value is not a markup extension, just store it as text for
                    // runtime resolution as a constructor parameter.
                    string value = (String)list[listIndex];
                    AttributeData nestedData = IsMarkupExtensionAttribute(data.DeclaringType,
                                                                          string.Empty,
                                                                      ref value,
                                                                          data.LineNumber,
                                                                          data.LinePosition,
                                                                          data.Depth,
                                                                          null);
                    if (nestedData == null)
                    {
                        RemoveEscapes(ref value);

                        xamlNodes.Add(new XamlTextNode(
                                        data.LineNumber,
                                        data.LinePosition,
                                        data.Depth,
                                        value,
                                        null));
                    }
                    else
                    {
                        CompileAttributeCore(xamlNodes, nestedData);
                    }
                }

                // End of constructor parameter section.
                xamlNodes.Add(new XamlConstructorParametersEndNode(
                                    data.LineNumber,
                                    data.LinePosition,
                                    data.Depth--));

#if PBTCOMPILER
                if (data.TargetType != typeof(UnknownMarkupExtension))
                {
                    // For compile mode, check that there is a constructor with the correct
                    // number of arguments.  In xaml load scenarios, the BamlRecordReader
                    // will do this, so don't bother doing it here.  If the target type is
                    // unknown, then we defer this check until it can be resolved.

                    ConstructorInfo[] infos = data.TargetType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
                    for (int i=0; i<infos.Length; i++)
                    {
                        ConstructorInfo info = infos[i];
                        ParameterInfo[] paramInfos = info.GetParameters();
                        if (paramInfos.Length == numberOfConstructorAttributes)
                        {
                            // Found a constructor with the right number of arguments
                            return;
                        }
                    }

                    // If we get to here, then no matching constructor was found, so complain
                    ThrowException(SRID.ParserBadConstructorParams, data.TargetType.Name,
                                   numberOfConstructorAttributes.ToString(CultureInfo.CurrentCulture),
                                   data.LineNumber, data.LinePosition);
                }

#endif
            }
        }

        /// <summary>
        /// At this point the start element is created and all the constructor params
        /// have been processed.  Now handle the name=value pairs as property sets.
        /// If we have used a regular start element record, then just use regular
        /// properties.
        /// </summary>

        private void WriteProperties(
            ArrayList        xamlNodes,
            ArrayList        list,
            int              listIndex,
            DefAttributeData data)
        {
            if (list != null && listIndex < list.Count)
            {
                ArrayList propertyNamesSoFar = new ArrayList(list.Count/4);

                for (int k = listIndex; k < list.Count; k+=4)
                {
                    if (k > (list.Count-3) ||
                        (list[k+1] is String) ||
                        ((Char)list[k+1]) != '=')
                    {
                        ThrowException(SRID.ParserMarkupExtensionNoNameValue, data.Args,
                                       data.LineNumber, data.LinePosition);
                    }


                    // See if this is a duplicate property definition, and throw if it is.

                    string propertyName = list[k] as String;
                    propertyName = propertyName.Trim();

                    if (propertyNamesSoFar.Contains(propertyName))
                    {
                        ThrowException(SRID.ParserDuplicateMarkupExtensionProperty, propertyName, data.LineNumber, data.LinePosition);
                    }
                    propertyNamesSoFar.Add( propertyName );

                    // Fetch the property context

                    int nameIndex = propertyName.IndexOf(':');
                    string localName = (nameIndex < 0) ? propertyName : propertyName.Substring(nameIndex+1);
                    string prefix = (nameIndex < 0) ? String.Empty : propertyName.Substring(0, nameIndex);

                    string attribNamespaceURI = ResolveAttributeNamespaceURI(prefix, localName, data.TargetNamespaceUri);

                    object dynamicObject;
                    string assemblyName;
                    string typeFullName;
                    Type   declaringType;
                    string dynamicObjectName;

                    AttributeContext attributeContext = GetAttributeContext(
                                                            data.TargetType,
                                                            data.TargetNamespaceUri,
                                                            attribNamespaceURI,
                                                            localName,
                                                        out dynamicObject,
                                                        out assemblyName,
                                                        out typeFullName,
                                                        out declaringType,
                                                        out dynamicObjectName);

                    // Handle nested markup extensions by recursing here.  If the
                    // value is not a markup extension, just store it as text for
                    // runtime resolution.
                    string strValue = list[k+2] as String;
                    AttributeData nestedAttrData = IsMarkupExtensionAttribute(
                                                        data.TargetType,
                                                        propertyName,
                                                    ref strValue,
                                                        data.LineNumber,
                                                        data.LinePosition,
                                                        data.Depth,
                                                        dynamicObject);

                    list[k+2] = strValue;
                    if (data.IsUnknownExtension)
                    {
                        // For unknown extensions, no more work should be done.  
                        // In pass1, we don't yet have the context to make sense of the nested properties, 
                        // so recursing into them would lead to spurious parse errors.
                        // In pass2 an unknown extension would have errored out before getting here.
                        return;
                    }

                    if (nestedAttrData != null)
                    {
                        if (nestedAttrData.IsSimple)
                        {
                            CompileProperty(xamlNodes,
                                            propertyName,
                                            nestedAttrData.Args,
                                            data.TargetType,
                                            data.TargetNamespaceUri,    // xmlns of TargetType
                                            nestedAttrData,
                                            nestedAttrData.LineNumber,
                                            nestedAttrData.LinePosition,
                                            nestedAttrData.Depth);
                        }
                        else
                        {
                            // NOTE: Consider checking validity of property by calling GetAttributeContext here.
                            CompileAttribute(xamlNodes, nestedAttrData);
                        }
                    }
                    else
                    {
                        CompileProperty(xamlNodes,
                                        propertyName,
                                        ((String)list[k+2]),
                                        data.TargetType,
                                        data.TargetNamespaceUri,    // xmlns of TargetType
                                        null,
                                        data.LineNumber,
                                        data.LinePosition,
                                        data.Depth);
                    }
                }
            }
        }

        private string ResolveAttributeNamespaceURI(string prefix, string name, string parentURI)
        {
            string attribNamespaceURI;
            if(!String.IsNullOrEmpty(prefix))
            {
                attribNamespaceURI = _parserHelper.LookupNamespace(prefix);
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
                    attribNamespaceURI = _parserHelper.LookupNamespace("");
            }
            return attribNamespaceURI;
        }

        /// <summary>
        /// Looks up the already constructed BracketCharacter cache for the BracketCharacters on 
        /// the given property.
        /// </summary>
        private SpecialBracketCharacters GetBracketCharacterForProperty(string propertyName, Dictionary<string, SpecialBracketCharacters> bracketCharacterCache)
        {
            SpecialBracketCharacters bracketCharacters = null;
            if (bracketCharacterCache != null && bracketCharacterCache.ContainsKey(propertyName))
            {
                bracketCharacters = bracketCharacterCache[propertyName];
            }

            return bracketCharacters;
        }
        
        /// <summary>
        /// Represent a single property for a MarkupExtension as a complex property.
        /// </summary>
        private void CompileProperty(
            ArrayList xamlNodes,
            string name,
            string value,
            Type parentType,
            string parentTypeNamespaceUri,
            AttributeData data,
            int lineNumber,
            int linePosition,
            int depth)
        {
            RemoveEscapes(ref name);
            RemoveEscapes(ref value);

            int nameIndex = name.IndexOf(':');
            string localName = (nameIndex < 0) ? name : name.Substring(nameIndex+1);
            string prefix = (nameIndex < 0) ? String.Empty : name.Substring(0, nameIndex);
            string attribNamespaceURI = ResolveAttributeNamespaceURI(prefix, localName, parentTypeNamespaceUri);

            object dynamicObject;
            string assemblyName;
            string typeFullName;
            Type   declaringType;
            string dynamicObjectName;

            if (String.IsNullOrEmpty(attribNamespaceURI))
            {
               ThrowException(SRID.ParserPrefixNSProperty, prefix, name, lineNumber, linePosition);
            }

            AttributeContext attributeContext = GetAttributeContext(
                                                    parentType,
                                                    parentTypeNamespaceUri,
                                                    attribNamespaceURI,
                                                    localName,
                                                out dynamicObject,
                                                out assemblyName,
                                                out typeFullName,
                                                out declaringType,
                                                out dynamicObjectName);

            if (attributeContext != AttributeContext.Property)
            {
                ThrowException(SRID.ParserMarkupExtensionUnknownAttr, localName,
                               parentType.FullName, lineNumber, linePosition);
            }

            MemberInfo info = dynamicObject as MemberInfo;

            Debug.Assert(null != info, "No property or method info for field Name");

            if (data != null && data.IsSimple)
            {
                if (data.IsTypeExtension)
                {
                    string typeValueFullName = value;  // set this to original value for error reporting if reqd.
                    string typeValueAssemblyFullName = null;
                    Type typeValue = _parserContext.XamlTypeMapper.GetTypeFromBaseString(value,
                                                                                         _parserContext,
                                                                                         true);
                    if (typeValue != null)
                    {
                        typeValueFullName = typeValue.FullName;
                        typeValueAssemblyFullName = typeValue.Assembly.FullName;
                    }

                    XamlPropertyWithTypeNode xamlPropertyWithTypeNode =
                        new XamlPropertyWithTypeNode(data.LineNumber,
                                                     data.LinePosition,
                                                     data.Depth,
                                                     dynamicObject,
                                                     assemblyName,
                                                     typeFullName,
                                                     localName,
                                                     typeValueFullName,
                                                     typeValueAssemblyFullName,
                                                     typeValue,
                                                     string.Empty,
                                                     string.Empty);

                    xamlNodes.Add(xamlPropertyWithTypeNode);
                }
                else
                {
                    XamlPropertyWithExtensionNode xamlPropertyWithExtensionNode =
                        new XamlPropertyWithExtensionNode(data.LineNumber,
                                                          data.LinePosition,
                                                          data.Depth,
                                                          dynamicObject,
                                                          assemblyName,
                                                          typeFullName,
                                                          localName,
                                                          value,
                                                          data.ExtensionTypeId,
                                                          data.IsValueNestedExtension,
                                                          data.IsValueTypeExtension);

                    xamlNodes.Add(xamlPropertyWithExtensionNode);
                }
            }
            else
            {
                XamlPropertyNode xamlPropertyNode =
                    new XamlPropertyNode(lineNumber,
                                         linePosition,
                                         depth,
                                         dynamicObject,
                                         assemblyName,
                                         typeFullName,
                                         dynamicObjectName,
                                         value,
                                         BamlAttributeUsage.Default,
                                         true);

                xamlNodes.Add(xamlPropertyNode);
            }
        }

        /// <summary>
        /// Remove any '\' escape characters from the passed string.  This does a simple
        /// pass through the string and won't do anything if there are no '\' characters.
        /// </summary>
        internal static void RemoveEscapes(ref string value)
        {
            StringBuilder builder=null;
            bool noEscape = true;
            for (int i = 0; i < value.Length; i++)
            {
                if (noEscape && value[i] == '\\')
                {
                    if (builder == null)
                    {
                        builder = new StringBuilder(value.Length);
                        builder.Append(value.Substring(0,i));
                    }
                    noEscape = false;
                }
                else if (builder != null)
                {
                    builder.Append(value[i]);
                    noEscape = true;
                }
            }

            if (builder != null)
            {
                value = builder.ToString();
            }
        }

        /// <summary>
        /// Get property information for an attribute in a MarkupExtension.  This is
        /// very similar code to what is done in XamlReaderHelper, but we only look for clr
        /// properties here, since MarkupExtensions don't support events or
        /// DependencyProperties.
        /// </summary>
        AttributeContext GetAttributeContext(
                Type   elementBaseType,
                string elementBaseTypeNamespaceUri,
                string attributeNamespaceUri,
                string attributeLocalName,
            out Object dynamicObject,        // resolved object.
            out string assemblyName,         // assemblyName the declaringType is found in
            out string typeFullName,         // typeFullName of the object that the field is on
            out Type   declaringType,        // type of the object that the field is on
            out string dynamicObjectName)    // name of the dynamicObject if found one
        {
            AttributeContext attributeContext = AttributeContext.Unknown;

            dynamicObject = null;
            assemblyName = null;
            typeFullName = null;
            declaringType = null;
            dynamicObjectName = null;

            // First, check if this is a CLR property using Static setter name
            // matching or property info lookups on element base type.
            MemberInfo mi = _parserContext.XamlTypeMapper.GetClrInfo(false,
                                              elementBaseType,
                                              attributeNamespaceUri,
                                              attributeLocalName,
                                          ref dynamicObjectName);

            if (null != mi)
            {
                attributeContext = AttributeContext.Property;
                dynamicObject = mi;
                declaringType = mi.DeclaringType;
                typeFullName = declaringType.FullName;
                assemblyName = declaringType.Assembly.FullName;
            }

            return attributeContext;
        }

        /// <summary>
        /// Throw a XamlParseException
        /// </summary>
        void ThrowException(
            string id,
            string parameter1,
            int    lineNumber,
            int    linePosition)
        {
            string message = SR.Get(id, parameter1);
            ThrowExceptionWithLine(message, lineNumber, linePosition);
        }

        /// <summary>
        /// Throw a XamlParseException
        /// </summary>
        void ThrowException(
            string id,
            string parameter1,
            string parameter2,
            int    lineNumber,
            int    linePosition)
        {
            string message = SR.Get(id, parameter1, parameter2);
            ThrowExceptionWithLine(message, lineNumber, linePosition);
        }

        /// <summary>
        /// Throw a XamlParseException
        /// </summary>
        void ThrowException(
            string id,
            string parameter1,
            string parameter2,
            string parameter3,
            int    lineNumber,
            int    linePosition)
        {
            string message = SR.Get(id, parameter1, parameter2, parameter3);
            ThrowExceptionWithLine(message, lineNumber, linePosition);
        }

        /// <summary>
        /// Throw a XamlParseException
        /// </summary>
        void ThrowExceptionWithLine(
            string message,
            int    lineNumber,
            int    linePosition)
        {
            message += " ";
            message += SR.Get(SRID.ParserLineAndOffset,
                              lineNumber.ToString(CultureInfo.CurrentCulture),
                              linePosition.ToString(CultureInfo.CurrentCulture));

            XamlParseException parseException = new XamlParseException(message,
                lineNumber, linePosition);

            throw parseException;
        }

        // Helper that provices namespace and type resolutions
        private IParserHelper    _parserHelper;

        // Parser Context for the current node being parsed.
        private ParserContext    _parserContext;

        internal class UnknownMarkupExtension
        {
        }
    }
}
