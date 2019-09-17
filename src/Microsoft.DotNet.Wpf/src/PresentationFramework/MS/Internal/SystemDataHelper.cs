// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Helper methods for code that uses types from System.Data.
//

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;

namespace MS.Internal
{
    internal static class SystemDataHelper
    {
        // return true if the list is a DataView
        internal static bool IsDataView(IBindingList list)
        {
            SystemDataExtensionMethods extensions = AssemblyHelper.ExtensionsForSystemData();
            return (extensions != null) ? extensions.IsDataView(list) : false;
        }

        // return true if the item is a DataRowView
        internal static bool IsDataRowView(object item)
        {
            SystemDataExtensionMethods extensions = AssemblyHelper.ExtensionsForSystemData();
            return (extensions != null) ? extensions.IsDataRowView(item) : false;
        }

        // return true if the value is null in the SqlTypes sense
        internal static bool IsSqlNull(object value)
        {
            SystemDataExtensionMethods extensions = AssemblyHelper.ExtensionsForSystemData();
            return (extensions != null) ? extensions.IsSqlNull(value) : false;
        }

        // return true if the type is nullable in the SqlTypes sense
        internal static bool IsSqlNullableType(Type type)
        {
            SystemDataExtensionMethods extensions = AssemblyHelper.ExtensionsForSystemData();
            return (extensions != null) ? extensions.IsSqlNullableType(type) : false;
        }

        // ADO DataSet exposes some properties that cause problems involving
        // identity and change notifications.  We handle these specially.
        internal static bool IsDataSetCollectionProperty(PropertyDescriptor pd)
        {
            SystemDataExtensionMethods extensions = AssemblyHelper.ExtensionsForSystemData();
            return (extensions != null) ? extensions.IsDataSetCollectionProperty(pd) : false;
        }

        // Intercept GetValue calls for certain ADO properties
        internal static object GetValue(object item, PropertyDescriptor pd, bool useFollowParent)
        {
            SystemDataExtensionMethods extensions = AssemblyHelper.ExtensionsForSystemData();
            return (extensions != null) ? extensions.GetValue(item, pd, useFollowParent) : null;
        }

        // return true if DBNull is a valid value for the given item and column.
        // The column may be specified directly by name, or indirectly by indexer: Item[arg]
        internal static bool DetermineWhetherDBNullIsValid(object item, string columnName, object arg)
        {
            SystemDataExtensionMethods extensions = AssemblyHelper.ExtensionsForSystemData();
            return (extensions != null) ? extensions.DetermineWhetherDBNullIsValid(item, columnName, arg) : false;
        }

        // return a null value appropriate for the given SqlNullable type
        internal static object NullValueForSqlNullableType(Type type)
        {
            // some SqlTypes are structs with a Null field.
            FieldInfo nullField = type.GetField("Null", BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);
            if (nullField != null)
            {
                return nullField.GetValue(null);
            }

            // Others are classes with a Null property.
            PropertyInfo nullProperty = type.GetProperty("Null", BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy);
            if (nullProperty != null)
            {
                return nullProperty.GetValue(null, null);
            }

            Debug.Assert(false, "Could not find Null field or property for SqlNullable type");
            return null;
        }
    }
}

