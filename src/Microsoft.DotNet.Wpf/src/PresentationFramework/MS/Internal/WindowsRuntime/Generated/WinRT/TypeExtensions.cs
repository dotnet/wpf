// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace WinRT
{
    internal static class TypeExtensions
    {
        private const string NamespacePrefix = "MS.Internal.WindowsRuntime.";

        public static Type FindHelperType(this Type type)
        {
            if (typeof(Exception).IsAssignableFrom(type))
            {
                type = typeof(Exception);
            }
            Type customMapping = Projections.FindCustomHelperTypeMapping(type);
            if (customMapping is not null)
            {
                return customMapping;
            }

            string helper = $"ABI.{type.FullName}";
            string helperTypeName2 = $"MS.Internal.WindowsRuntime.ABI.{type.FullName}";
            if (type.FullName.StartsWith(NamespacePrefix, StringComparison.Ordinal))
            {
                helper = $"MS.Internal.WindowsRuntime.ABI.{RemoveNamespacePrefix(type.FullName)}";
            }

            return Type.GetType(helper) ?? Type.GetType(helperTypeName2);
        }

        public static Type GetHelperType(this Type type)
        {
            var helperType = type.FindHelperType();
            if (helperType is not null)
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
            if (helperType.IsGenericType && vftblType is not null)
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
            return ns.StartsWith(NamespacePrefix, StringComparison.Ordinal) ? ns.Substring(NamespacePrefix.Length) : ns;
        }
    }
}
