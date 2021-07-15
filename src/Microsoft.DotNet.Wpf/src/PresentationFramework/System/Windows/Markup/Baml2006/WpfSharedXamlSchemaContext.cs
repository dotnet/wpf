// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Xaml;

namespace System.Windows.Baml2006
{
    internal class WpfSharedXamlSchemaContext : WpfSharedBamlSchemaContext
    {
        // V3 Rules are:
        //  Simple Collection rules: We only lookup IList & IDictionary (no add methods) (The MarkupCompiler doesn't support this)
        //  No Deferring Loader lookup on XamlMember (The MarkupCompiler doesn't support this)
        public WpfSharedXamlSchemaContext(XamlSchemaContextSettings settings, bool useV3Rules) : base(settings)
        {
            _useV3Rules = useV3Rules;
        }

        public override XamlType GetXamlType(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            XamlType xType;
             
            lock (_syncObject)
            {
                if (!_masterTypeTable.TryGetValue(type, out xType))
                {
                    RequireRuntimeType(type);
                    xType = CreateKnownBamlType(type.Name, false, _useV3Rules);
                    if (xType == null || xType.UnderlyingType != type)
                    {
                        xType = new WpfXamlType(type, this, false /* isBamlType */, _useV3Rules);
                    }
                    _masterTypeTable.Add(type, xType);
                }
            }

            return xType;
        }

        internal static void RequireRuntimeType(Type type)
        {
            // To avoid injection of derived System.Types that lie about their identity
            // (and spoof other types), only allow RuntimeTypes.
            // S.W.M.XamlReader only supports live reflection, anyway.
            Type runtimeType = typeof(object).GetType();
            if (!runtimeType.IsAssignableFrom(type.GetType()))
            {
                throw new ArgumentException(SR.Get(SRID.RuntimeTypeRequired, type), "type");
            }
        }

        // Allow wrapping SchemaContexts a way to call into the protected overload of GetXamlType
        internal XamlType GetXamlTypeInternal(string xamlNamespace, string name, params XamlType[] typeArguments)
        {
            return base.GetXamlType(xamlNamespace, name, typeArguments);
        }

        private Dictionary<Type, XamlType> _masterTypeTable = new Dictionary<Type, XamlType>();
        private object _syncObject = new Object();
        private bool _useV3Rules;
    }
}
