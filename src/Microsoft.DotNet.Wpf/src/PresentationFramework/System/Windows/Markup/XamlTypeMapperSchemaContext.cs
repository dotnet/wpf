// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Windows.Baml2006;
using System.Xaml;
using MS.Internal;
using MS.Utility;

namespace System.Windows.Markup
{
    public partial class XamlTypeMapper
    {
        internal class XamlTypeMapperSchemaContext : XamlSchemaContext
        {
            // Initialized in constructor
            Dictionary<string, FrugalObjectList<string>> _nsDefinitions;
            XamlTypeMapper _typeMapper;
            WpfSharedXamlSchemaContext _sharedSchemaContext;

            // Lock on syncObject
            object syncObject = new object();
            Dictionary<string, string> _piNamespaces;
            IEnumerable<string> _allXamlNamespaces;
            Dictionary<Type, XamlType> _allowedInternalTypes;
            HashSet<string> _clrNamespaces;

            internal XamlTypeMapperSchemaContext(XamlTypeMapper typeMapper)
                : base() 
            {
                _typeMapper = typeMapper;
                _sharedSchemaContext = (WpfSharedXamlSchemaContext)XamlReader.GetWpfSchemaContext();

                // Copy all the NamespaceMapEntrys from our parent into _nsDefinitions
                if (typeMapper._namespaceMaps != null)
                {
                    _nsDefinitions = new Dictionary<string, FrugalObjectList<string>>();
                    foreach (NamespaceMapEntry mapEntry in _typeMapper._namespaceMaps)
                    {
                        FrugalObjectList<string> clrNsList;
                        if (!_nsDefinitions.TryGetValue(mapEntry.XmlNamespace, out clrNsList))
                        {
                            clrNsList = new FrugalObjectList<string>(1);
                            _nsDefinitions.Add(mapEntry.XmlNamespace, clrNsList);
                        }
                        string clrNs = GetClrNsUri(mapEntry.ClrNamespace, mapEntry.AssemblyName);
                        clrNsList.Add(clrNs);
                    }
                }

                // Copy all the PIs from our parent into _piNamespaces
                if (typeMapper.PITable.Count > 0)
                {
                    _piNamespaces = new Dictionary<string, string>(typeMapper.PITable.Count);
                    foreach (DictionaryEntry entry in typeMapper.PITable)
                    {
                        ClrNamespaceAssemblyPair pair = (ClrNamespaceAssemblyPair)entry.Value;
                        string clrNs = GetClrNsUri(pair.ClrNamespace, pair.AssemblyName);
                        _piNamespaces.Add((string)entry.Key, clrNs);
                    }
                }

                _clrNamespaces = new HashSet<string>();
            }

            // Return all the namespaces gathered via reflection, plus any additional ones from user mappings
            public override IEnumerable<string> GetAllXamlNamespaces()
            {
                IEnumerable<string> result = _allXamlNamespaces;
                if (result == null)
                {
                    lock (syncObject)
                    {
                        if (_nsDefinitions != null || _piNamespaces != null)
                        {
                            // Use shared schema context, to avoid redundant reflection
                            List<string> resultList = new List<string>(_sharedSchemaContext.GetAllXamlNamespaces());
                            AddKnownNamespaces(resultList);
                            result = resultList.AsReadOnly();
                        }
                        else
                        {
                            result = _sharedSchemaContext.GetAllXamlNamespaces();
                        }
                        _allXamlNamespaces = result;
                    }
                }
                return result;
            }

            public override XamlType GetXamlType(Type type)
            {
                if (ReflectionHelper.IsPublicType(type))
                {
                    return _sharedSchemaContext.GetXamlType(type);
                }
                return GetInternalType(type, null);
            }

            public override bool TryGetCompatibleXamlNamespace(string xamlNamespace, out string compatibleNamespace)
            {
                // XmlnsCompat mappings only come from reflection, so get them from the shared SchemaContext
                if (_sharedSchemaContext.TryGetCompatibleXamlNamespace(xamlNamespace, out compatibleNamespace))
                {
                    return true;
                }
                // Otherwise, if we have a map or a PI for the namespace, then treat it as known
                if (_nsDefinitions != null && _nsDefinitions.ContainsKey(xamlNamespace) ||
                    _piNamespaces != null && SyncContainsKey(_piNamespaces, xamlNamespace))
                {
                    compatibleNamespace = xamlNamespace;
                    return true;
                }
                return false;
            }

            // Returns a Hashtable of ns -> NamespaceMapEntry[] containing all mappings that are
            // different from what would be returned by the shared context.
            // We don't use _typeMapper.NamespaceMapHashList because we don't want to duplicate all
            // the reflection done by the shared SchemaContext.
            // Thread safety: we assume this is called on the parser thread, and so it's safe to
            // iterate the _typeMapper's data structures.
            internal Hashtable GetNamespaceMapHashList()
            {
                // Copy the ctor-provided namespace mappings into the result
                Hashtable result = new Hashtable();
                if (_typeMapper._namespaceMaps != null)
                {
                    foreach (NamespaceMapEntry mapEntry in _typeMapper._namespaceMaps)
                    {
                        NamespaceMapEntry clone = new NamespaceMapEntry
                        {
                            XmlNamespace = mapEntry.XmlNamespace,
                            ClrNamespace = mapEntry.ClrNamespace,
                            AssemblyName = mapEntry.AssemblyName,
                            AssemblyPath = mapEntry.AssemblyPath
                        };
                        AddToMultiHashtable(result, mapEntry.XmlNamespace, clone);
                    }
                }

                // Copy the Mapping PIs into the result
                foreach (DictionaryEntry piEntry in _typeMapper.PITable)
                {
                    ClrNamespaceAssemblyPair mapping = (ClrNamespaceAssemblyPair)piEntry.Value;
                    NamespaceMapEntry mapEntry = new NamespaceMapEntry
                    {
                        XmlNamespace = (string)piEntry.Key,
                        ClrNamespace = mapping.ClrNamespace,
                        AssemblyName = mapping.AssemblyName,
                        AssemblyPath = _typeMapper.AssemblyPathFor(mapping.AssemblyName)
                    };
                    AddToMultiHashtable(result, mapEntry.XmlNamespace, mapEntry);
                }

                // Add any clr-namespaces that were resolved using custom assembly paths
                lock (syncObject)
                {
                    foreach (string clrNs in _clrNamespaces)
                    {
                        string clrNamespace, assembly;
                        SplitClrNsUri(clrNs, out clrNamespace, out assembly);
                        if (!string.IsNullOrEmpty(assembly))
                        {
                            string assemblyPath = _typeMapper.AssemblyPathFor(assembly);
                            if (!string.IsNullOrEmpty(assemblyPath))
                            {
                                NamespaceMapEntry mapEntry = new NamespaceMapEntry
                                {
                                    XmlNamespace = clrNs,
                                    ClrNamespace = clrNamespace,
                                    AssemblyName = assembly,
                                    AssemblyPath = assemblyPath
                                };
                                AddToMultiHashtable(result, mapEntry.XmlNamespace, mapEntry);
                            }
                        }
                    }
                }

                // Convert all the lists to arrays
                object[] keys = new object[result.Count];
                result.Keys.CopyTo(keys, 0);
                foreach (object key in keys)
                {
                    List<NamespaceMapEntry> list = (List<NamespaceMapEntry>)result[key];
                    result[key] = list.ToArray();
                }

                return result;
            }

            internal void SetMappingProcessingInstruction(string xamlNamespace, ClrNamespaceAssemblyPair pair)
            {
                string clrNs = GetClrNsUri(pair.ClrNamespace, pair.AssemblyName);
                lock (syncObject)
                {
                    if (_piNamespaces == null)
                    {
                        _piNamespaces = new Dictionary<string, string>();
                    }
                    _piNamespaces[xamlNamespace] = clrNs;

                    // We potentially have a new namespace, so invalidate the cached list of namespaces
                    _allXamlNamespaces = null;
                }
            }

            protected override XamlType GetXamlType(string xamlNamespace, string name, params XamlType[] typeArguments)
            {
                try
                {
                    return LookupXamlType(xamlNamespace, name, typeArguments);
                }
                catch (Exception e)
                {
                    if (CriticalExceptions.IsCriticalException(e))
                    {
                        throw;
                    }
                    if (_typeMapper.LoadReferenceAssemblies())
                    {
                        // If new reference assemblies were loaded, retry the type load
                        return LookupXamlType(xamlNamespace, name, typeArguments);
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            // Load assembly using the user-provided path, if available
            protected override Assembly OnAssemblyResolve(string assemblyName)
            {
                string assemblyPath = _typeMapper.AssemblyPathFor(assemblyName);
                if (!string.IsNullOrEmpty(assemblyPath))
                {
                    return ReflectionHelper.LoadAssembly(assemblyName, assemblyPath);
                }
                return base.OnAssemblyResolve(assemblyName);
            }

            private static string GetClrNsUri(string clrNamespace, string assembly)
            {
                return XamlReaderHelper.MappingProtocol + clrNamespace +
                    XamlReaderHelper.MappingAssembly + assembly;
            }

            private static void SplitClrNsUri(string xmlNamespace, out string clrNamespace, out string assembly)
            {
                clrNamespace = null;
                assembly = null;
                int clrNsIndex = xmlNamespace.IndexOf(XamlReaderHelper.MappingProtocol, StringComparison.Ordinal);
                if (clrNsIndex < 0)
                {
                    return;
                }
                clrNsIndex += XamlReaderHelper.MappingProtocol.Length;
                if (clrNsIndex <= xmlNamespace.Length)
                {
                    return;
                }
                int assemblyIndex = xmlNamespace.IndexOf(XamlReaderHelper.MappingAssembly, StringComparison.Ordinal);
                if (assemblyIndex < clrNsIndex)
                {
                    clrNamespace = xmlNamespace.Substring(clrNsIndex);
                    return;
                }
                clrNamespace = xmlNamespace.Substring(clrNsIndex, assemblyIndex - clrNsIndex);
                assemblyIndex += XamlReaderHelper.MappingAssembly.Length;
                if (assemblyIndex <= xmlNamespace.Length)
                {
                    return;
                }
                assembly = xmlNamespace.Substring(assemblyIndex);
            }

            // Should be called within lock (syncObject)
            private void AddKnownNamespaces(List<string> nsList)
            {
                if (_nsDefinitions != null)
                {
                    foreach (string ns in _nsDefinitions.Keys)
                    {
                        if (!nsList.Contains(ns))
                        {
                            nsList.Add(ns);
                        }
                    }
                }
                if (_piNamespaces != null)
                {
                    foreach (string ns in _piNamespaces.Keys)
                    {
                        if (!nsList.Contains(ns))
                        {
                            nsList.Add(ns);
                        }
                    }
                }
            }

            private XamlType GetInternalType(Type type, XamlType sharedSchemaXamlType)
            {
                lock (syncObject)
                {
                    if (_allowedInternalTypes == null)
                    {
                        _allowedInternalTypes = new Dictionary<Type, XamlType>();
                    }

                    XamlType result;
                    if (!_allowedInternalTypes.TryGetValue(type, out result))
                    {
                        WpfSharedXamlSchemaContext.RequireRuntimeType(type);
                        if (_typeMapper.IsInternalTypeAllowedInFullTrust(type))
                        {
                            // Return a type that claims to be public, so that XXR doesn't filter it out.
                            result = new VisibilityMaskingXamlType(type, _sharedSchemaContext);
                        }
                        else
                        {
                            result = sharedSchemaXamlType ?? _sharedSchemaContext.GetXamlType(type);
                        }
                        _allowedInternalTypes.Add(type, result);
                    }
                    return result;
                }
            }

            // Tries to use the user-provided mappings to resolve a type. Note that the returned type
            // won't include the mapping in its list of namespaces; that is not necessary for v3 compat.
            //
            // Note that for anything other than reflected mappings, we don't use the shared SchemaContext,
            // because we want to make sure that our OnAssemblyResolve handler is called.
            private XamlType LookupXamlType(string xamlNamespace, string name, XamlType[] typeArguments)
            {
                // First look through the user-provided namespace mappings
                XamlType result;
                FrugalObjectList<string> clrNsList;
                if (_nsDefinitions != null && _nsDefinitions.TryGetValue(xamlNamespace, out clrNsList))
                {
                    for (int i = 0; i < clrNsList.Count; i++)
                    {
                        result = base.GetXamlType(clrNsList[i], name, typeArguments);
                        if (result != null)
                        {
                            return result;
                        }
                    }
                }

                // Then look for a PI
                string piMappingClrNs;
                if (_piNamespaces != null && SyncTryGetValue(_piNamespaces, xamlNamespace, out piMappingClrNs))
                {
                    return base.GetXamlType(piMappingClrNs, name, typeArguments);
                }

                // Then see if it's a CLR namespace
                if (xamlNamespace.StartsWith(XamlReaderHelper.MappingProtocol, StringComparison.Ordinal))
                {
                    lock (syncObject)
                    {
                        if (!_clrNamespaces.Contains(xamlNamespace))
                        {
                            _clrNamespaces.Add(xamlNamespace);
                        }
                    }
                    return base.GetXamlType(xamlNamespace, name, typeArguments);
                }

                // Finally, use the reflected xmlnsdefs (get them from the shared SchemaContext, to avoid redundant reflection)
                result = _sharedSchemaContext.GetXamlTypeInternal(xamlNamespace, name, typeArguments);
                // Apply visibility filtering, because the shared SchemaContext can't do that.
                return result == null ||  result.IsPublic ? result : GetInternalType(result.UnderlyingType, result);
            }

            private bool SyncContainsKey<K,V>(IDictionary<K, V> dict, K key)
            {
                lock (syncObject)
                {
                    return dict.ContainsKey(key);
                }
            }

            private bool SyncTryGetValue(IDictionary<string, string> dict, string key, out string value)
            {
                lock (syncObject)
                {
                    return dict.TryGetValue(key, out value);
                }
            }

            private static void AddToMultiHashtable<K, V>(Hashtable hashtable, K key, V value)
            {
                List<V> list = (List<V>)hashtable[key];
                if (list == null)
                {
                    list = new List<V>();
                    hashtable.Add(key, list);
                }
                list.Add(value);
            }
        }

        // This XamlType claims to be public, so that XXR won't filter out internals that are
        // allowed by XamlTypeMapper.AllowInternalType
        private class VisibilityMaskingXamlType : XamlType
        {
            public VisibilityMaskingXamlType(Type underlyingType, XamlSchemaContext schemaContext)
                : base(underlyingType, schemaContext)
            {
            }

            protected override bool LookupIsPublic()
            {
                return true;
            }
        }
    }
}




