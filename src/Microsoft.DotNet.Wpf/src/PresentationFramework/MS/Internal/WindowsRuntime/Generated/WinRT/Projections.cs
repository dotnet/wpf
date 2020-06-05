// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Numerics;
using System.Reflection;
using System.Threading;
using System.Windows.Input;

namespace WinRT
{
    internal static class Projections
    {
        private static readonly ReaderWriterLockSlim rwlock = new ReaderWriterLockSlim();
        private static readonly Dictionary<Type, Type> CustomTypeToHelperTypeMappings = new Dictionary<Type, Type>();
        private static readonly Dictionary<Type, Type> CustomAbiTypeToTypeMappings = new Dictionary<Type, Type>();
        private static readonly Dictionary<string, Type> CustomAbiTypeNameToTypeMappings = new Dictionary<string, Type>();
        private static readonly Dictionary<Type, string> CustomTypeToAbiTypeNameMappings = new Dictionary<Type, string>();
        private static readonly HashSet<string> ProjectedRuntimeClassNames = new HashSet<string>();

        static Projections()
        {
            RegisterCustomAbiTypeMappingNoLock(typeof(bool), typeof(ABI.System.Boolean), "Boolean");
            RegisterCustomAbiTypeMappingNoLock(typeof(char), typeof(ABI.System.Char), "Char");
            RegisterCustomAbiTypeMappingNoLock(typeof(IReadOnlyList<>), typeof(MS.Internal.WindowsRuntime.ABI.System.Collections.Generic.IReadOnlyList<>), "Windows.Foundation.Collections.IVectorView`1");
        }

        private static void RegisterCustomAbiTypeMappingNoLock(Type publicType, Type abiType, string winrtTypeName, bool isRuntimeClass = false)
        {
            CustomTypeToHelperTypeMappings.Add(publicType, abiType);
            CustomAbiTypeToTypeMappings.Add(abiType, publicType);
            CustomTypeToAbiTypeNameMappings.Add(publicType, winrtTypeName);
            CustomAbiTypeNameToTypeMappings.Add(winrtTypeName, publicType);
            if (isRuntimeClass)
            {
                ProjectedRuntimeClassNames.Add(winrtTypeName);
            }
        }

        public static Type FindCustomHelperTypeMapping(Type publicType)
        {
            rwlock.EnterReadLock();
            try
            {
                if (publicType.IsGenericType)
                {
                    return CustomTypeToHelperTypeMappings.TryGetValue(publicType.GetGenericTypeDefinition(), out Type abiTypeDefinition)
                        ? abiTypeDefinition.MakeGenericType(publicType.GetGenericArguments())
                        : null;
                }
                return CustomTypeToHelperTypeMappings.TryGetValue(publicType, out Type abiType) ? abiType : null;
            }
            finally
            {
                rwlock.ExitReadLock();
            }
        }

        public static Type FindCustomPublicTypeForAbiType(Type abiType)
        {
            rwlock.EnterReadLock();
            try
            {
                if (abiType.IsGenericType)
                {
                    return CustomAbiTypeToTypeMappings.TryGetValue(abiType.GetGenericTypeDefinition(), out Type publicTypeDefinition)
                        ? publicTypeDefinition.MakeGenericType(abiType.GetGenericArguments())
                        : null;
                }
                return CustomAbiTypeToTypeMappings.TryGetValue(abiType, out Type publicType) ? publicType : null;
            }
            finally
            {
                rwlock.ExitReadLock();
            }
        }

        public static Type FindCustomTypeForAbiTypeName(string abiTypeName)
        {
            rwlock.EnterReadLock();
            try
            {
                return CustomAbiTypeNameToTypeMappings.TryGetValue(abiTypeName, out Type type) ? type : null;
            }
            finally
            {
                rwlock.ExitReadLock();
            }
        }

        public static string FindCustomAbiTypeNameForType(Type type)
        {
            rwlock.EnterReadLock();
            try
            {
                return CustomTypeToAbiTypeNameMappings.TryGetValue(type, out string typeName) ? typeName : null;
            }
            finally
            {
                rwlock.ExitReadLock();
            }
        }

        public static bool IsTypeWindowsRuntimeType(Type type)
        {
            Type typeToTest = type;
            if (typeToTest.IsArray)
            {
                typeToTest = typeToTest.GetElementType();
            }
            return IsTypeWindowsRuntimeTypeNoArray(typeToTest);
        }

        private static bool IsTypeWindowsRuntimeTypeNoArray(Type type)
        {
            if (type.IsConstructedGenericType)
            {
                if(IsTypeWindowsRuntimeTypeNoArray(type.GetGenericTypeDefinition()))
                {
                    foreach (var arg in type.GetGenericArguments())
                    {
                        if (!IsTypeWindowsRuntimeTypeNoArray(arg))
                        {
                            return false;
                        }
                    }
                    return true;
                }
                return false;
            }
            return CustomTypeToAbiTypeNameMappings.ContainsKey(type)
                || type.IsPrimitive
                || type == typeof(string)
                || type == typeof(Guid)
                || type == typeof(object)
                || type.GetCustomAttribute<WindowsRuntimeTypeAttribute>() is object;
        }

        public static bool TryGetCompatibleWindowsRuntimeTypeForVariantType(Type type, out Type compatibleType)
        {
            compatibleType = null;
            if (!type.IsConstructedGenericType)
            {
                throw new ArgumentException(nameof(type));
            }

            var definition = type.GetGenericTypeDefinition();

            if (!IsTypeWindowsRuntimeTypeNoArray(definition))
            {
                return false;
            }

            var genericConstraints = definition.GetGenericArguments();
            var genericArguments = type.GetGenericArguments();
            var newArguments = new Type[genericArguments.Length];
            for (int i = 0; i < genericArguments.Length; i++)
            {
                if (!IsTypeWindowsRuntimeTypeNoArray(genericArguments[i]))
                {
                    bool argumentCovariant = (genericConstraints[i].GenericParameterAttributes & GenericParameterAttributes.VarianceMask) == GenericParameterAttributes.Covariant;
                    if (argumentCovariant && !genericArguments[i].IsValueType)
                    {
                        newArguments[i] = typeof(object);
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    newArguments[i] = genericArguments[i];
                }
            }
            compatibleType = definition.MakeGenericType(newArguments);
            return true;
        }

        internal static bool TryGetDefaultInterfaceTypeForRuntimeClassType(Type runtimeClass, out Type defaultInterface)
        {
            defaultInterface = null;
            ProjectedRuntimeClassAttribute attr = runtimeClass.GetCustomAttribute<ProjectedRuntimeClassAttribute>();
            if (attr is null)
            {
                return false;
            }

            defaultInterface = runtimeClass.GetProperty(attr.DefaultInterfaceProperty, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly).PropertyType;
            return true;
        }

        internal static Type GetDefaultInterfaceTypeForRuntimeClassType(Type runtimeClass)
        {
            if (!TryGetDefaultInterfaceTypeForRuntimeClassType(runtimeClass, out Type defaultInterface))
            {
                throw new ArgumentException($"The provided type '{runtimeClass.FullName}' is not a WinRT projected runtime class.", nameof(runtimeClass));
            }
            return defaultInterface;
        }

        internal static bool TryGetMarshalerTypeForProjectedRuntimeClass(IObjectReference objectReference, out Type type)
        {
            if(objectReference.TryAs<IInspectable.Vftbl>(out var inspectablePtr) == 0)
            {
                rwlock.EnterReadLock();
                try
                {
                    IInspectable inspectable = inspectablePtr;
                    string runtimeClassName = inspectable.GetRuntimeClassName(true);
                    if (runtimeClassName is object)
                    {
                        if (ProjectedRuntimeClassNames.Contains(runtimeClassName))
                        {
                            type = CustomTypeToHelperTypeMappings[CustomAbiTypeNameToTypeMappings[runtimeClassName]];
                            return true;
                        }
                    }
                }
                finally
                {
                    inspectablePtr.Dispose();
                    rwlock.ExitReadLock();
                }
            }
            type = null;
            return false;
        }
    }
}
