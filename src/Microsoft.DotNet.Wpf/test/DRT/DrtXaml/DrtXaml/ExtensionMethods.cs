// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Xaml;
using System.Xaml.Schema;

namespace DrtXaml
{
    internal static class ExtensionMethods
    {
        internal static XamlType GetXamlType(
            this XamlSchemaContext schemaContext, string xamlNamespace, string name)
        {
            XamlTypeName typeName = new XamlTypeName(xamlNamespace, name);
            return schemaContext.GetXamlType(typeName);
        }

        internal static string GetAssemblyName(this Type type)
        {
            return type.Assembly.GetName().Name;
        }
    }
}
