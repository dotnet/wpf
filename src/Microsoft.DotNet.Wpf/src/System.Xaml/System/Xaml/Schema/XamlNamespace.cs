// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Xaml.MS.Impl;
using MS.Internal.Xaml.Parser;

namespace System.Xaml.Schema
{
    internal class XamlNamespace
    {
        public readonly XamlSchemaContext SchemaContext;

        private List<AssemblyNamespacePair> _assemblyNamespaces;
        private ConcurrentDictionary<string, XamlType> _typeCache;
        private ICollection<XamlType> _allPublicTypes;

        public bool IsClrNamespace { get; private set; }

        public XamlNamespace(XamlSchemaContext schemaContext)
        {
            SchemaContext = schemaContext;
        }

        // This ctor is used for "clr-namespace" uri's where there is only one pair
        public XamlNamespace(XamlSchemaContext schemaContext, string clrNs, string assemblyName)
        {
            SchemaContext = schemaContext;
            _assemblyNamespaces = GetClrNamespacePair(clrNs, assemblyName);
            // For now we just ignore failures, including swalloing assembly load exceptions.
            // Any types in this namespace will be treated as unknown. But it would be useful to
            // surface errors here through tracing or an event.
            if (_assemblyNamespaces != null)
            {
                Initialize();
            }
            IsClrNamespace = true;
        }

        private void Initialize()
        {
            _typeCache = XamlSchemaContext.CreateDictionary<string, XamlType>();
        }

        public bool IsResolved => _assemblyNamespaces != null;

        public ICollection<XamlType> GetAllXamlTypes() => _allPublicTypes ??= LookupAllTypes();

        public XamlType GetXamlType(string typeName, params XamlType[] typeArgs)
        {
            if (!IsResolved)
            {
                return null;
            }

            if (typeArgs == null || typeArgs.Length == 0)
            {
                return TryGetXamlType(typeName) ?? TryGetXamlType(GetTypeExtensionName(typeName));
            }
            
            Type[] clrTypeArgs = ConvertArrayOfXamlTypesToTypes(typeArgs);
            return TryGetXamlType(typeName, clrTypeArgs) ?? TryGetXamlType(GetTypeExtensionName(typeName), clrTypeArgs);
        }

        private XamlType TryGetXamlType(string typeName)
        {
            // Look up the type in our cache. If it's there, we're done.
            XamlType xamlType;
            if (_typeCache.TryGetValue(typeName, out xamlType))
            {
                return xamlType;
            }

            // Otherwise, look up the type via reflection
            Type type = TryGetType(typeName);
            if (type == null)
            {
                return null;
            }

            // And save it in our cache
            xamlType = SchemaContext.GetXamlType(type);
            if (xamlType == null)
            {
                return null;
            }

            return XamlSchemaContext.TryAdd(_typeCache, typeName, xamlType);
        }

        private XamlType TryGetXamlType(string typeName, Type[] typeArgs)
        {
            Debug.Assert(typeArgs.Length > 0, "This method should only be called for generic types.");

            // It is not possible to get an array of open generic and then call
            // MakeGenericType on it so we need to process array subscripts.
            string subscript;
            typeName = GenericTypeNameScanner.StripSubscript(typeName, out subscript);
            typeName = MangleGenericTypeName(typeName, typeArgs.Length);

            // Get the open generic type.
            XamlType openXamlType = TryGetXamlType(typeName);
            Type openType = openXamlType?.UnderlyingType;
            if (openType == null)
            {
                return null;
            }

            // Close the open generic type.
            Type closedType = openType.MakeGenericType(typeArgs);
            if (!string.IsNullOrEmpty(subscript))
            {
                closedType = MakeArrayType(closedType, subscript);
                if (closedType == null)
                {
                    // Invalid array subscript.
                    return null;
                }
            }

            return SchemaContext.GetXamlType(closedType);
        }

        private static Type MakeArrayType(Type elementType, string subscript)
        {
            Type type = elementType;
            int pos = 0;
            do
            {
                int rank = GenericTypeNameScanner.ParseSubscriptSegment(subscript, ref pos);
                if (rank == 0)
                {
                    // Invalid array subscript.
                    return null;
                }

                type = (rank == 1) ? type.MakeArrayType() : type.MakeArrayType(rank);
            }
            while (pos < subscript.Length);
            return type;
        }

        private static string MangleGenericTypeName(string typeName, int paramNum)
        {
            return typeName + KnownStrings.GraveQuote + paramNum;
        }

        private Type[] ConvertArrayOfXamlTypesToTypes(XamlType[] typeArgs)
        {
            var clrTypeArgs = new Type[typeArgs.Length];
            for (int n = 0; n < typeArgs.Length; n++)
            {
                // Checking for nulls and unknowns is done in public API layer before we ever get here
                Debug.Assert(typeArgs[n] != null);
                Debug.Assert(typeArgs[n].UnderlyingType != null);

                clrTypeArgs[n] = typeArgs[n].UnderlyingType;
            }
            return clrTypeArgs;
        }

        internal int RevisionNumber
        {
            // The only external mutation we allow is adding new namespaces. So the count of
            // namespaces also serves as a revision number.
            get => (_assemblyNamespaces != null) ? _assemblyNamespaces.Count : 0;
        }

        private Type TryGetType(string typeName)
        {
            Type type = SearchAssembliesForShortName(typeName);
            if (type == null && IsClrNamespace)
            {
                Debug.Assert(_assemblyNamespaces.Count == 1);
                type = XamlLanguage.LookupClrNamespaceType(_assemblyNamespaces[0], typeName);
            }
            if (type == null)
            {
                return null;
            }

            // Discard private nested types
            Type currentType = type;
            while (currentType.IsNested)
            {
                if (currentType.IsNestedPrivate)
                {
                    return null;
                }
                currentType = currentType.DeclaringType;
            }
            return type;
        }

        private ICollection<XamlType> LookupAllTypes()
        {
            List<XamlType> xamlTypeList = new List<XamlType>();
            if (IsResolved)
            {
                foreach (AssemblyNamespacePair assemblyNamespacePair in _assemblyNamespaces)
                {
                    Assembly asm = assemblyNamespacePair.Assembly;
                    if (asm == null)
                    {
                        // This is a dynamic assembly that got unloaded; ignore it
                        continue;
                    }
                    string clrPrefix = assemblyNamespacePair.ClrNamespace;

                    Type[] types = asm.GetTypes();

                    foreach (Type t in types)
                    {
                        if (!KS.Eq(t.Namespace, clrPrefix))
                            continue;

                        XamlType xamlType = SchemaContext.GetXamlType(t);
                        xamlTypeList.Add(xamlType);
                    }
                }
            }
            return xamlTypeList.AsReadOnly();
        }

        private List<AssemblyNamespacePair> GetClrNamespacePair(string clrNs, string assemblyName)
        {
            Assembly asm = SchemaContext.OnAssemblyResolve(assemblyName);
            if (asm == null)
            {
                return null;
            }

            List<AssemblyNamespacePair> onePair = new List<AssemblyNamespacePair>();
            onePair.Add(new AssemblyNamespacePair(asm, clrNs));
            return onePair;
        }

        private Type SearchAssembliesForShortName(string shortName)
        {
            foreach(AssemblyNamespacePair assemblyNamespacePair in _assemblyNamespaces)
            {
                Assembly asm = assemblyNamespacePair.Assembly;
                if (asm == null)
                {
                    // This is a dynamic assembly that got unloaded; ignore it
                    continue;
                }
                string longName = assemblyNamespacePair.ClrNamespace + "." + shortName;

                Type type = asm.GetType(longName);
                if (type != null)
                {
                    return type;
                }
            }
            return null;
        }

        // This method should only be called inside SchemaContext._syncExaminingAssemblies lock
        internal void AddAssemblyNamespacePair(AssemblyNamespacePair pair)
        {
            // To allow the list to be read by multiple threads, we create a new list, add the pair, 
            // then assign it back to the original variable.  Assignments are assured to be atomic.

            List<AssemblyNamespacePair> assemblyNamespacesCopy;
            if (_assemblyNamespaces == null)
            {
                assemblyNamespacesCopy = new List<AssemblyNamespacePair>();
                Initialize();
            }
            else
            {
                assemblyNamespacesCopy = new List<AssemblyNamespacePair>(_assemblyNamespaces);
            }

            assemblyNamespacesCopy.Add(pair);
            _assemblyNamespaces = assemblyNamespacesCopy;
        }

        private string GetTypeExtensionName(string typeName) => typeName + KnownStrings.Extension;
    }
}
