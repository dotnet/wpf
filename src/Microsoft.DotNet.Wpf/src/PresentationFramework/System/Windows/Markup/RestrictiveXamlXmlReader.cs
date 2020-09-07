// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿//
// Description: This class provides a XamlXmlReader implementation that skips over some known dangerous 
// types when calling into the Read method, this is meant to prevent WpfXamlLoader from instantiating.
//

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Xaml;
using System.Xml;
using System.Security;

namespace System.Windows.Markup
{
    /// <summary>
    /// Provides a XamlXmlReader implementation that that skips over some known dangerous types.
    /// </summary>
    internal class RestrictiveXamlXmlReader : System.Xaml.XamlXmlReader
    {
        /// <summary>
        /// The RestrictedTypes in the _restrictedTypes list do not initially contain a Type reference, we use a Type reference to determine whether an incoming Type can be assigned to that Type
        /// in order to restrict it or allow it. We cannot get a Type reference without loading the assembly, so we need to first go through the loaded assemblies, and assign the Types
        /// that are loaded.
        /// </summary>
        static RestrictiveXamlXmlReader()
        {
            _unloadedTypes = new ConcurrentDictionary<string, List<RestrictedType>>();

            // Go through the list of restricted types and add each type under the matching assembly entry in the dictionary.
            foreach (RestrictedType type in _restrictedTypes)
            {
                if (!String.IsNullOrEmpty(type.AssemblyName))
                {
                    if(!_unloadedTypes.ContainsKey(type.AssemblyName))
                    {
                        _unloadedTypes[type.AssemblyName] = new List<RestrictedType>();
                    }

                    _unloadedTypes[type.AssemblyName].Add(type);
                }
                else
                {
                    // If the RestrictedType entry does not provide an assembly name load it from PresentationFramework,
                    // this is the current assembly so it's already loaded, just get the Type and assign it to the RestrictedType entry
                    System.Type typeReference = System.Type.GetType(type.TypeName, false);

                    if (typeReference != null)
                    {
                        type.TypeReference = typeReference;
                    }
                }
            }
        }

        /// <summary>
        /// Builds the restricted set based on RestrictedTypes that have already been loaded.
        /// </summary>
        public RestrictiveXamlXmlReader(XmlReader xmlReader, XamlSchemaContext schemaContext, XamlXmlReaderSettings settings) : base(xmlReader, schemaContext, settings)
        {
        }

        /// <summary>
        /// Builds the restricted set based on RestrictedTypes that have already been loaded but adds the list of Types passed in in safeTypes to the instance of _safeTypesSet
        /// </summary>
        internal RestrictiveXamlXmlReader(XmlReader xmlReader, XamlSchemaContext schemaContext, XamlXmlReaderSettings settings, List<Type> safeTypes) : base(xmlReader, schemaContext, settings)
        {
            if (safeTypes != null)
            {
                foreach (Type safeType in safeTypes)
                {
                    _safeTypesSet.Add(safeType);
                }
            }
        }
        /// <summary>

        /// Calls the base Read method to extract a node from the Xaml parser, if it's found to be a StartObject node for a type we want to restrict we skip that node.
        /// </summary>
        /// <returns>
        /// Returns the next available Xaml node skipping over dangerous types.
        /// </returns>
        public override bool Read()
        {
            bool result = false;
            int skippingDepth = 0;

            while (result = base.Read())
            {
                if (skippingDepth <= 0)
                {
                    if (NodeType == System.Xaml.XamlNodeType.StartObject &&
                        IsRestrictedType(Type.UnderlyingType))
                    {
                        skippingDepth = 1;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    if (NodeType == System.Xaml.XamlNodeType.StartObject ||
                        NodeType == System.Xaml.XamlNodeType.GetObject)
                    {
                        skippingDepth += 1;
                    }
                    else if (NodeType == System.Xaml.XamlNodeType.EndObject)
                    {
                        skippingDepth -= 1;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Determines whether an incoming type is either a restricted type or inheriting from one.
        /// </summary>
        private bool IsRestrictedType(Type type)
        {
            if (type != null)
            {
                // If an incoming type is already in the set we can just return false, we've verified this type is safe.
                if (_safeTypesSet.Contains(type))
                {
                    return false;
                }

                // Ensure that the restricted type list has the latest information for the assemblies currently loaded.
                EnsureLatestAssemblyLoadInformation();

                // Iterate through our _restrictedTypes list, if an entry has a TypeReference then the type is loaded and we can check it.
                foreach (RestrictedType restrictedType in _restrictedTypes)
                {
                    if (restrictedType.TypeReference?.IsAssignableFrom(type) == true)
                    {
                        return true;
                    }
                }

                // We've detected this type isn't nor inherits from a restricted type, add it to the safe types set.
                _safeTypesSet.Add(type);
            }

            return false;
        }

        /// <summary>
        /// Iterates through the currently loaded assemblies and gets the Types for the assemblies we've marked as unloaded.
        /// If our thread static assembly count is still the same we can skip this, we know no new assemblies have been loaded.
        /// </summary>
        private static void EnsureLatestAssemblyLoadInformation()
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            if (assemblies.Length != _loadedAssembliesCount)
            {
                foreach (Assembly assembly in assemblies)
                {
                    RegisterAssembly(assembly);
                }

                _loadedAssembliesCount = assemblies.Length;
            }
        }


        /// <summary>
        /// Get the Types from a newly loaded assembly and assign them in the RestrictedType list.
        /// </summary>
        private static void RegisterAssembly(Assembly assembly)
        {
            if (assembly != null)
            {
                string fullName = assembly.FullName;

                List<RestrictedType> types = null;
                if (_unloadedTypes.TryGetValue(fullName, out types))
                {
                    if (types != null)
                    {
                        foreach (RestrictedType restrictedType in types)
                        {
                            Type typeInfo = assembly.GetType(restrictedType.TypeName, false);
                            restrictedType.TypeReference = typeInfo;
                        }
                    }

                    _unloadedTypes.TryRemove(fullName, out types);
                }
            }
        }

        /// <summary>
        /// Known dangerous types exploitable through XAML load.
        /// </summary>
        static List<RestrictedType> _restrictedTypes = new List<RestrictedType>() {
                    new RestrictedType("System.Windows.Data.ObjectDataProvider",""),
                    new RestrictedType("System.Windows.ResourceDictionary",""),
                    new RestrictedType("System.Configuration.Install.AssemblyInstaller","System.Configuration.Install, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"),
                    new RestrictedType("System.Activities.Presentation.WorkflowDesigner","System.Activities.Presentation, Version = 4.0.0.0, Culture = neutral, PublicKeyToken = 31bf3856ad364e35"),
                    new RestrictedType("System.Windows.Forms.BindingSource","System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")
                };
        /// <summary>
        /// Dictionary to keep track of known types that have not yet been loaded, the key is the assembly name.
        /// Once an assembly is loaded we take the known types in that assembly, set the Type property in the RestrictedType entry, then remove the assembly entry.
        /// </summary>
        static ConcurrentDictionary<string, List<RestrictedType>> _unloadedTypes = null;
        
        /// <summary>
        /// Per instance set of found restricted types, this is initialized to the known types that have been loaded,
        /// if a type is found that is not already in the set and is assignable to a restricted type it is added.
        /// </summary>
        HashSet<Type> _safeTypesSet = new HashSet<Type>();

        /// <summary>
        /// Keeps track of the assembly count, if the assembly count is the same we avoid having to go through the loaded assemblies.
        /// Once we get a Type in IsRestrictedType we are guaranteed to have already loaded it's assembly and base type assemblies,any other
        /// assembly loads happening in other threads during our processing of IsRestrictedType do not affect our ability to check the Type properly.
        /// </summary>
        [ThreadStatic] private static int _loadedAssembliesCount;

        /// <summary>
        /// Helper class to store type names.
        /// </summary>
        private class RestrictedType
        {
            public RestrictedType(string typeName, string assemblyName)
            {
                TypeName = typeName;
                AssemblyName = assemblyName;
            }

            public string TypeName { get; set; }
            public string AssemblyName { get; set; }
            public Type TypeReference { get; set; }
        }
    }
}


