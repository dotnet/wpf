// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace WinRT
{

    internal static class TypeExtensions
    {
        public static Type FindHelperType(this Type type)
        {
            if (typeof(Exception).IsAssignableFrom(type))
            {
                type = typeof(Exception);
            }
            Type customMapping = Projections.FindCustomHelperTypeMapping(type);
            if (customMapping is object)
            {
                return customMapping;
            }
            var helper = $"ABI.{type.FullName}";
            string helperTypeName2 = $"MS.Internal.WindowsRuntime.ABI.{type.FullName}";
            if (type.FullName.StartsWith("MS.Internal.WindowsRuntime."))
            {
                helper = "MS.Internal.WindowsRuntime.ABI." + RemoveNamespacePrefix(type.FullName);
            }
            return Type.GetType(helper) ?? Type.GetType(helperTypeName2);
        }

        public static Type GetHelperType(this Type type)
        {
            var helperType = type.FindHelperType();
            if (helperType is object)
                return helperType;
            throw new InvalidOperationException($"Target type is not a projected type: {type.FullName}.");
        }

        public static Type GetGuidType(this Type type)
        {
            return type.IsDelegate() ? type.GetHelperType() : type;
        }

        public static Type FindVftblType(this Type helperType)
        {
            Type vftblType = helperType.GetNestedType("Vftbl");
            if (vftblType is null)
            {
                return null;
            }
            if (helperType.IsGenericType && vftblType is object)
            {
                vftblType = vftblType.MakeGenericType(helperType.GetGenericArguments());
            }
            return vftblType;
        }

        public static Type GetAbiType(this Type type)
        {
            return type.GetHelperType().GetMethod("GetAbi", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).ReturnType;
        }

        public static Type GetMarshalerType(this Type type)
        {
            return type.GetHelperType().GetMethod("CreateMarshaler", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).ReturnType;
        }

        public static bool IsDelegate(this Type type)
        {
            return typeof(Delegate).IsAssignableFrom(type);
        }

        public static string RemoveNamespacePrefix(string ns)
        {
            const string NamespacePrefix = "MS.Internal.WindowsRuntime.";
            if (ns.StartsWith(NamespacePrefix))
            {
                return ns.Substring(NamespacePrefix.Length);
            }
            return ns;
        }
    }
}
