// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿using System;
using System.IO;
using System.Net;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.ComponentModel;
using System.Windows.Threading;
using System.Windows.Media;
using System.IO.Packaging;
using MS.Internal.IO.Packaging;         // for PackageCacheEntry
using System.Globalization;
using System.Windows.Navigation;

using MS.Internal;
using MS.Internal.Utility;
using MS.Internal.AppModel;
using MS.Utility;
using System.Xaml;
using System.Windows.Baml2006;
using System.Windows.Markup;

namespace System.Windows
{
    public class DeferrableContentConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (typeof(Stream).IsAssignableFrom(sourceType) || sourceType == typeof(byte[]))
            {
                return true;
            }

            return base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value != null)
            {
                if (context == null)
                {
                    throw new ArgumentNullException("context");
                }

                XamlSchemaContext xamlSchemaContext =
                    RequireService<IXamlSchemaContextProvider>(context).SchemaContext;
                Baml2006SchemaContext schemaContext = xamlSchemaContext as Baml2006SchemaContext;
                if (schemaContext == null)
                {
                    throw new InvalidOperationException(SR.Get(SRID.ExpectedBamlSchemaContext));
                }

                IXamlObjectWriterFactory objectWriterFactory =
                    RequireService<IXamlObjectWriterFactory>(context);
                IProvideValueTarget ipvt =
                    RequireService<IProvideValueTarget>(context);
                IRootObjectProvider rootObjectProvider =
                    RequireService<IRootObjectProvider>(context);

                ResourceDictionary dictionary = ipvt.TargetObject as ResourceDictionary;
                if (dictionary == null)
                {
                    throw new InvalidOperationException(SR.Get(SRID.ExpectedResourceDictionaryTarget));
                }

                Stream stream = value as Stream;
                if (stream == null)
                {
                    byte[] bytes = value as byte[];
                    if (bytes != null)
                    {
                        stream = new MemoryStream(bytes);
                    }
                }
                if (stream == null)
                {
                    throw new InvalidOperationException(SR.Get(SRID.ExpectedBinaryContent));
                }

                // we shouldn't pass around the service provider
                DeferrableContent deferrableContext = new DeferrableContent(stream, schemaContext,
                    objectWriterFactory, context, rootObjectProvider.RootObject);
                return deferrableContext;
            }

            return base.ConvertFrom(context, culture, value);
        }

        private static T RequireService<T>(IServiceProvider provider) where T : class
        {
            T result = provider.GetService(typeof(T)) as T;
            if (result == null)
            {
                throw new InvalidOperationException(SR.Get(SRID.DeferringLoaderNoContext, typeof(DeferrableContentConverter).Name, typeof(T).Name));
            }
            return result;
        }
    }
}
