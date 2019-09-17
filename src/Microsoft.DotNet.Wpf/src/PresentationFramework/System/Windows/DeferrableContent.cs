// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Security;
using System.Xaml;
using System.Xaml.Permissions;
using System.Windows.Baml2006;
using System.ComponentModel;

namespace System.Windows
{
    [TypeConverter(typeof(DeferrableContentConverter))]
    public class DeferrableContent
    {
        internal Stream Stream
        {
            get;
            private set;
        }

        internal Baml2006SchemaContext SchemaContext { get; private set; }
        internal IXamlObjectWriterFactory ObjectWriterFactory { get; private set; }
        internal XamlObjectWriterSettings ObjectWriterParentSettings { get; private set; }
        internal object RootObject { get; private set; }

        // we shouldn't be passing this around since it's not guaranteed to be
        // valid outside the scope of a DeferringContentLoader.Load call.
        internal IServiceProvider ServiceProvider { get; private set; }

        internal DeferrableContent(Stream stream, Baml2006SchemaContext schemaContext, 
            IXamlObjectWriterFactory objectWriterFactory, IServiceProvider serviceProvider,
            object rootObject)
        {
            ObjectWriterParentSettings = objectWriterFactory.GetParentSettings();
            bool assemblyTargetsFramework2 = false;
            // The local assembly can be null if it is not specified in the XamlReaderSettings.
            if (schemaContext.LocalAssembly != null)
            {
                assemblyTargetsFramework2 = schemaContext.LocalAssembly.ImageRuntimeVersion.StartsWith("v2", StringComparison.Ordinal);
            }
            // There is an incompatibility between the framework versions 3 and 4 regarding MarkupExtension resources.
            // In version 3, MarkupExtension resources did not provide values when looked up.
            // In version 4, they do.
            if (assemblyTargetsFramework2)
            {
                ObjectWriterParentSettings.SkipProvideValueOnRoot = true;
            }
            this.Stream = stream;
            this.SchemaContext = schemaContext;
            this.ObjectWriterFactory = objectWriterFactory;
            this.ServiceProvider = serviceProvider;
            this.RootObject = rootObject;
        }
    }
}
