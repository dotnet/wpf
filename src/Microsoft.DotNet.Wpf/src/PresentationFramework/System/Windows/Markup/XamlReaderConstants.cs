// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***************************************************************************\
*
* Purpose:  Constants for XAML system.  Kept in XamlReaderHelper class.
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
    internal partial class XamlReaderHelper
    {
        // Define for the Definition NamespaceURI so not hardcoded everywhere.
        internal const string DefinitionNamespaceURI = "http://schemas.microsoft.com/winfx/2006/xaml";
        internal const string DefinitionUid = "Uid";
        internal const string DefinitionType = "Type";
        internal const string DefinitionTypeArgs = "TypeArguments";
        internal const string DefinitionName = "Key";
        internal const string DefinitionRuntimeName = "Name";
        internal const string DefinitionShared = "Shared";
        internal const string DefinitionSynchronousMode = "SynchronousMode";
        internal const string DefinitionAsyncRecords = "AsyncRecords";
        internal const string DefinitionContent = "Content";
        internal const string DefinitionClass = "Class";
        internal const string DefinitionSubclass = "Subclass";
        internal const string DefinitionClassModifier = "ClassModifier";
        internal const string DefinitionFieldModifier = "FieldModifier";
        internal const string DefinitionCodeTag = "Code";
        internal const string DefinitionXDataTag = "XData";
        internal const string MappingProtocol = "clr-namespace:";
        internal const string MappingAssembly = ";assembly=";
        internal const string PresentationOptionsFreeze = "Freeze";

        // Default URI for Avalon base and framework.
        internal const string DefaultNamespaceURI = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

        // Default URI for Metro.  Note that this is used to map Key attribute for resource
        // dictionaries only. 
        internal const string DefinitionMetroNamespaceURI = "http://schemas.microsoft.com/xps/2005/06/resourcedictionary-key";

        // URI for WPF parsing options (currently only used for option to Freeze Freezables)
        internal const string PresentationOptionsNamespaceURI = "http://schemas.microsoft.com/winfx/2006/xaml/presentation/options";

#if !PBTCOMPILER
        internal static System.Xaml.XamlDirective Freeze
        {
            get
            {
                if (_freezeDirective == null)
                {
                    _freezeDirective = new System.Xaml.XamlDirective(XamlReaderHelper.PresentationOptionsNamespaceURI, XamlReaderHelper.PresentationOptionsFreeze);
                }

                return _freezeDirective;
            }
        }
        private static System.Xaml.XamlDirective _freezeDirective;
#endif 
    }
}
