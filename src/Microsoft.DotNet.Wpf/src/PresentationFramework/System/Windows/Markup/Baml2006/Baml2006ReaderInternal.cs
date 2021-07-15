// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Xaml;
using System.Diagnostics;
using System.ComponentModel;
using System.Reflection;
using System.Windows.Diagnostics;
using System.Windows.Media.Media3D;
using System.Windows.Media;
using MS.Internal;
using System.Globalization;
using XamlReaderHelper = System.Windows.Markup.XamlReaderHelper;

namespace System.Windows.Baml2006
{
    // This class exists to resolve an ambiguity between different versions of the same assembly when loading resource dictionaries.
    // We couldn't modify the original Baml2006Reader to fix this issue because it is public, instead we created this class, and use it when loading dictionaries.
    internal class Baml2006ReaderInternal : Baml2006Reader
    {
        #region Constructors

        internal Baml2006ReaderInternal(Stream stream,
            Baml2006SchemaContext schemaContext,
            Baml2006ReaderSettings settings) : base(stream, schemaContext, settings)
        {
        }
        
        internal Baml2006ReaderInternal(
            Stream stream,
            Baml2006SchemaContext baml2006SchemaContext,
            Baml2006ReaderSettings baml2006ReaderSettings,
            object root)
            : base(stream, baml2006SchemaContext, baml2006ReaderSettings, root)
        {
        }

        #endregion

        // Return the full assembly name, this includes the assembly version
        internal override string GetAssemblyNameForNamespace(Assembly asm)
        {
            return asm.FullName;
        }

        // When processing ResourceDictionary.Source we may find a Uri that references the
        // local assembly, but if another version of the same assembly is loaded we may have trouble resolving
        // to the correct one. this method returns a custom markup extension that passes down the local assembly 
        // information to help in this case.
        internal override object CreateTypeConverterMarkupExtension(XamlMember property, TypeConverter converter, object propertyValue, Baml2006ReaderSettings settings)
        {
            if (FrameworkAppContextSwitches.AppendLocalAssemblyVersionForSourceUri &&
                property.DeclaringType.UnderlyingType == typeof(System.Windows.ResourceDictionary) &&
                property.Name.Equals("Source"))
            {
                return new SourceUriTypeConverterMarkupExtension(converter, propertyValue, settings.LocalAssembly);
            }

            return base.CreateTypeConverterMarkupExtension(property, converter, propertyValue, settings);
        }
    }
}
