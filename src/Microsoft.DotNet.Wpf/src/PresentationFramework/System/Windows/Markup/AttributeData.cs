// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***************************************************************************\
*
* Purpose:  AttributeContext and  *AttributeData
*
\***************************************************************************/

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
    /// Enumeration for different Attribute Context values.
    /// </summary>
    internal enum AttributeContext
    {
        Unknown,
        Property,
        RoutedEvent,
        ClrEvent,
        Code,
    }

    #region CompactSyntaxData

    // Class to cache attribute information relating to compact syntax properties,
    // so that they can be expanded into complex notation after processing all the
    // 'regular' properties.
    internal class AttributeData : DefAttributeData
    {
        internal AttributeData(
            string targetAssemblyName,
            string targetFullName,
            Type targetType,
            string args,
            Type declaringType,
            string propertyName,
            object info,
            Type serializerType,
            int lineNumber,
            int linePosition,
            int depth,
            string targetNamespaceUri,
            short extensionTypeId,
            bool isValueNestedExtension,
            bool isValueTypeExtension,
            bool isSimple) :
            base(targetAssemblyName, targetFullName, targetType, args, declaringType,
                 targetNamespaceUri, lineNumber, linePosition, depth, isSimple)
        {
            PropertyName = propertyName;
            SerializerType = serializerType;
            ExtensionTypeId = extensionTypeId;
            IsValueNestedExtension = isValueNestedExtension;
            IsValueTypeExtension = isValueTypeExtension;
            Info = info;
        }

        internal string PropertyName;         // Name of this property.
        internal Type SerializerType;         // Type of serializer (if any)
        internal short ExtensionTypeId;       // TypeId of a simple ME when it is the value of a property.
        internal bool IsValueNestedExtension; // True if the value of a simple ME is another simple ME.
        internal bool IsValueTypeExtension;   // True if the value of a simple ME is a TypeExtension.
        internal object Info;             // DependencyProperty or MethodInfo or ClrInfo associated with property to set.

        internal bool IsTypeExtension
        {
            get
            {
                return ExtensionTypeId == (short)KnownElements.TypeExtension;
            }
        }

        internal bool IsStaticExtension
        {
            get
            {
                return ExtensionTypeId == (short)KnownElements.StaticExtension;
            }
        }
    }

    // Class to cache attribute information relating to complex def attribute
    // syntax properties, so that they can be expanded into complex notation
    // after processing all the 'regular' properties.
    internal class DefAttributeData
    {
        internal DefAttributeData(
            string targetAssemblyName,
            string targetFullName,
            Type targetType,
            string args,
            Type declaringType,
            string targetNamespaceUri,
            int lineNumber,
            int linePosition,
            int depth,
            bool isSimple)
        {
            TargetType = targetType;
            DeclaringType = declaringType;
            TargetFullName = targetFullName;
            TargetAssemblyName = targetAssemblyName;
            Args = args;
            TargetNamespaceUri = targetNamespaceUri;
            LineNumber = lineNumber;
            LinePosition = linePosition;
            Depth = depth;
            IsSimple = isSimple;
        }

        internal Type TargetType;            // Target type
        internal Type DeclaringType;         // Type where Attribute is declared
        internal string TargetFullName;        // Full string name of target type
        internal string TargetAssemblyName;    // Assembly name where TargetType is defined
        internal string Args;                  // arguments with *typename() stripped off
        internal string TargetNamespaceUri; // xmlns namespace uri of the TargetType ME that owns propertyName.
        internal int LineNumber;            // Line for this attribute
        internal int LinePosition;          // Position for this attribute
        internal int Depth;                 // Xml element depth of start of attribute
        internal bool IsSimple;              // True if the attribute is a simple property

        internal bool IsUnknownExtension
        {
            get
            {
                return TargetType == typeof(MarkupExtensionParser.UnknownMarkupExtension);
            }
        }
    }

    #endregion CompactSyntaxData
}
