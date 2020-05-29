// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//   Handles local caching operations on the XmlnsCache file used for parsing
//

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using MS.Internal;
using MS.Utility;
using Microsoft.Win32;
using System.Globalization;
#if PBTCOMPILER
using MS.Internal.PresentationBuildTasks;
#else
using MS.Internal.PresentationFramework;
using MS.Internal.Utility;  // AssemblyCacheEnum
#endif

// Since we disable PreSharp warnings in this file, we first need to disable warnings about unknown message numbers and unknown pragmas.
#pragma warning disable 1634, 1691

#if PBTCOMPILER
namespace MS.Internal.Markup
#else
namespace System.Windows.Markup
#endif
{
    internal class XmlnsCache
    {
#if PBTCOMPILER
        static XmlnsCache()
        {
            // if the value of any assembly key entry in _assemblyHasCacheInfo is
            // false - don't look for xmlnsdefinitionAttribute in that assembly.
            // if it is true or there is no entry then we need to reflect for this
            // custom attribute.

            _assemblyHasCacheInfo["WINDOWSBASE"] = true;
            _assemblyHasCacheInfo["SYSTEM"] = false;
            _assemblyHasCacheInfo["SYSTEM.DATA"] = false;
            _assemblyHasCacheInfo["SYSTEM.XML"] = false;
            _assemblyHasCacheInfo["UIAUTOMATIONPROVIDER"] = false;
            _assemblyHasCacheInfo["UIAUTOMATIONTYPES"] = false;

            _assemblyHasCacheInfo["PRESENTATIONCORE"] = true;
            _assemblyHasCacheInfo["PRESENTATIONFRAMEWORK"] = true;
        }

        // Create a new instance of the namespace / assembly cache.
        // Use only the assemblies that are contained in the passed table,
        // where keys are assembly names, and values are paths.
        internal XmlnsCache(HybridDictionary assemblyPathTable)
        {
            InitializeWithReferencedAssemblies(assemblyPathTable);
        }

        private void InitializeWithReferencedAssemblies(HybridDictionary assemblyPathTable)
        {
            _compatTable = new Dictionary<string,string>();
            _compatTableReverse = new Dictionary<string,string>();
            _cacheTable = new HybridDictionary();
            _assemblyPathTable = assemblyPathTable;

            AddReferencedAssemblies();
        }

        // Table where key is assembly name and value is the file path where it should be found
        private HybridDictionary _assemblyPathTable = null;

        // Table where key is assembly name and value indicates if assembly contains any XmlnsDefinitionAttributes
        private static Hashtable _assemblyHasCacheInfo = new Hashtable(8);

        // Reflect on any assemblies that can be found in the passed table and look for
        // XmlnsDefinitionAttributes.  Add these to the cache table.
        private void AddReferencedAssemblies()
        {
            if (_assemblyPathTable == null || _assemblyPathTable.Count == 0)
            {
                return;
            }

            List<Assembly> interestingAssemblies = new List<Assembly>();

            // Load all the assemblies into a list.
            foreach(string assemblyName in _assemblyPathTable.Keys)
            {
                bool hasCacheInfo = true;
                Assembly assy;

                if (_assemblyHasCacheInfo[assemblyName] != null)
                {
                   hasCacheInfo = (bool)_assemblyHasCacheInfo[assemblyName];
                }
                if (!hasCacheInfo)
                {
                    continue;
                }
                assy = ReflectionHelper.GetAlreadyReflectionOnlyLoadedAssembly(assemblyName);
                if (assy == null)
                {
                    string assemblyFullPath = _assemblyPathTable[assemblyName] as string;

                    //
                    // The assembly path set from Compiler must be a full file path, which includes
                    // directory, file name and extension.
                    // Don't need to try different file extensions here.
                    //
                    if (!String.IsNullOrEmpty(assemblyFullPath) && File.Exists(assemblyFullPath))
                    {
                        assy = ReflectionHelper.LoadAssembly(assemblyName, assemblyFullPath);
                    }
                }
                if (assy != null)
                {
                    interestingAssemblies.Add(assy);
                }
            }

            Assembly[] asmList = interestingAssemblies.ToArray();
            LoadClrnsToAssemblyNameMappingCache(asmList);
            ProcessXmlnsCompatibleWithAttributes(asmList);
        }

#else

        internal XmlnsCache()
        {
            _compatTable = new Dictionary<string, string>();
            _compatTableReverse = new Dictionary<string, string>();
            _cacheTable = new HybridDictionary();
            _uriToAssemblyNameTable = new HybridDictionary();
        }
#endif

#if PBTCOMPILER
        // In the COmpiler everything is taken care of up front (all the
        // assemblies are listed in the PROJ file).  So the cache will be
        // fully populated and ready.
        internal List<ClrNamespaceAssemblyPair> GetMappingArray(string xmlns)
        {
            return _cacheTable[xmlns] as List<ClrNamespaceAssemblyPair>;
        }

#else
        // At runtime the cache is lazily populated as XmlNs URI's are
        // encounted in the BAML.  If the cache line is loaded return what
        // you find, other wise go load the cache.
        internal List<ClrNamespaceAssemblyPair> GetMappingArray(string xmlns)
        {
            List<ClrNamespaceAssemblyPair> clrNsMapping =null;
            lock(this)
            {
                clrNsMapping = _cacheTable[xmlns] as List<ClrNamespaceAssemblyPair>;
                if (clrNsMapping == null)
                {
                    if (_uriToAssemblyNameTable[xmlns] != null)
                    {
                        //
                        // if the xmlns maps to a list of assembly names which are saved in the baml records,
                        // try to get the mapping from those assemblies.
                        //

                        string[] asmNameList = _uriToAssemblyNameTable[xmlns] as string[];

                        Assembly[] asmList = new Assembly[asmNameList.Length];
                        for (int i = 0; i < asmNameList.Length; i++)
                        {
                            asmList[i] = ReflectionHelper.LoadAssembly(asmNameList[i], null);
                        }
                        _cacheTable[xmlns] = GetClrnsToAssemblyNameMappingList(asmList, xmlns);
                    }
                    else
                    {
                        //
                        // The xmlns uri doesn't map to a list of assemblies, this might be for
                        // regular xaml loading.
                        //
                        // Get the xmlns - clrns mapping from the currently loaded assemblies.
                        //
                        Assembly[] asmList = AppDomain.CurrentDomain.GetAssemblies();
                        _cacheTable[xmlns] = GetClrnsToAssemblyNameMappingList(asmList, xmlns);
                        ProcessXmlnsCompatibleWithAttributes(asmList);
                    }
                    clrNsMapping = _cacheTable[xmlns] as List<ClrNamespaceAssemblyPair>;
                }
            }
            return clrNsMapping;
        }

        internal void SetUriToAssemblyNameMapping(string namespaceUri, string [] asmNameList)
        {
            _uriToAssemblyNameTable[namespaceUri] = asmNameList;
        }
#endif

        // Return the new compatible namespace, given an old namespace
        internal string GetNewXmlnamespace(string oldXmlnamespace)
        {
            string newXmlNamespace;

            if (_compatTable.TryGetValue(oldXmlnamespace, out newXmlNamespace))
            {
                return newXmlNamespace;
            }

            return null;
        }

#if PBTCOMPILER
        private CustomAttributeData[] GetAttributes(Assembly asm, string fullClrName)
        {
            IList<CustomAttributeData> allAttributes = CustomAttributeData.GetCustomAttributes(asm);
            List<CustomAttributeData> foundAttributes = new List<CustomAttributeData>();
            for(int i=0; i<allAttributes.Count; i++)
            {
                // get the Constructor info
                CustomAttributeData data = allAttributes[i];
                ConstructorInfo cinfo = data.Constructor;
                if(0 == String.CompareOrdinal(cinfo.ReflectedType.FullName, fullClrName))
                {
                    foundAttributes.Add(allAttributes[i]);
                }
            }
            return foundAttributes.ToArray();
        }

        private void GetNamespacesFromDefinitionAttr(CustomAttributeData data, out string xmlns, out string clrns, out string assemblyName)
        {
            // typedConstructorArguments (the Attribute constructor arguments)
            // [MyAttribute("test", Name=Hello)]
            // "test" is the Constructor Argument
            xmlns = null;
            clrns = null;
            assemblyName = null;
            IList<CustomAttributeTypedArgument> constructorArguments = data.ConstructorArguments;
            for (int i = 0; i<constructorArguments.Count; i++)
            {
                CustomAttributeTypedArgument tca = constructorArguments[i];
                if (i == 0)
                    xmlns = tca.Value as String;
                else if (i == 1)
                    clrns = tca.Value as String;
                else
                    throw new ArgumentException(SR.Get(SRID.ParserAttributeArgsHigh, "XmlnsDefinitionAttribute"));
            }
            IList<CustomAttributeNamedArgument> namedArguments = data.NamedArguments;
            for (int i = 0; i < namedArguments.Count; i++)
            {
                var namedArgument = namedArguments[i];
                if (namedArgument.MemberName == "AssemblyName")
                {
                    assemblyName = namedArgument.TypedValue.Value as String;
                }
            }
        }

        private void GetNamespacesFromCompatAttr(CustomAttributeData data, out string oldXmlns, out string newXmlns)
        {
            // typedConstructorArguments (the Attribute constructor arguments)
            // [MyAttribute("test", Name=Hello)]
            // "test" is the Constructor Argument
            oldXmlns = null;
            newXmlns = null;
            IList<CustomAttributeTypedArgument> constructorArguments = data.ConstructorArguments;
            for (int i=0; i<constructorArguments.Count; i++)
            {
                CustomAttributeTypedArgument tca = constructorArguments[i];
                if (i == 0)
                    oldXmlns = tca.Value as String;
                else if (i == 1)
                    newXmlns = tca.Value as String;
                else
                    throw new ArgumentException(SR.Get(SRID.ParserAttributeArgsHigh, "XmlnsCompatibleWithAttribute"));
            }
        }

#else

        private Attribute[] GetAttributes(Assembly asm, Type attrType)
        {
            return Attribute.GetCustomAttributes(asm, attrType);
        }

        private void GetNamespacesFromDefinitionAttr(Attribute attr, out string xmlns, out string clrns, out string assemblyName)
        {
            XmlnsDefinitionAttribute xmlnsAttr  = (XmlnsDefinitionAttribute)attr;
            xmlns = xmlnsAttr.XmlNamespace;
            clrns = xmlnsAttr.ClrNamespace;
            assemblyName = xmlnsAttr.AssemblyName;
        }

        private void GetNamespacesFromCompatAttr(Attribute attr, out string oldXmlns, out string newXmlns)
        {
            XmlnsCompatibleWithAttribute xmlnsCompat = (XmlnsCompatibleWithAttribute)attr;
            oldXmlns = xmlnsCompat.OldNamespace;
            newXmlns = xmlnsCompat.NewNamespace;
        }
#endif

#if PBTCOMPILER
        // Load all the XmlnsDefinitionAttributes from ALL the Assemblies in
        // the cache all at one time.   (The compiler is driven off a PROJ file
        // that will list all the Assemblies it needs all up front.
        private void LoadClrnsToAssemblyNameMappingCache(Assembly[] asmList)
        {
            List<ClrNamespaceAssemblyPair> pairList;
            // For each assembly, enmerate all the XmlnsDefinition attributes.
            for(int asmIdx=0; asmIdx<asmList.Length; asmIdx++)
            {
                string assemblyName = asmList[asmIdx].FullName;
                CustomAttributeData[] attributes = GetAttributes(asmList[asmIdx],
                                                "System.Windows.Markup.XmlnsDefinitionAttribute");
                for(int attrIdx=0; attrIdx<attributes.Length; attrIdx++)
                {
                    string xmlns = null;
                    string clrns = null;
                    string assmname = null;
                    GetNamespacesFromDefinitionAttr(attributes[attrIdx], out xmlns, out clrns, out assmname);

                    if (String.IsNullOrEmpty(xmlns) || String.IsNullOrEmpty(clrns) )
                    {
                        throw new ArgumentException(SR.Get(SRID.ParserAttributeArgsLow, "XmlnsDefinitionAttribute"));
                    }

                    if (!_cacheTable.Contains(xmlns))
                    {
                        _cacheTable[xmlns] = new List<ClrNamespaceAssemblyPair>();
                    }
                    pairList = _cacheTable[xmlns] as List<ClrNamespaceAssemblyPair>;
                    pairList.Add(new ClrNamespaceAssemblyPair(clrns, assmname ?? assemblyName));
                }
            }
        }

#else
        // Get a list of (clrNs, asmName) pairs for a given XmlNs from a
        // given list of Assemblies.  This returns a list that the caller
        // Will add to the _cacheTable.   At runtime the needed assemblies
        // appear with various XmlNs namespaces one at a time as we read the BAML
        private List<ClrNamespaceAssemblyPair> GetClrnsToAssemblyNameMappingList(
                                                    Assembly[] asmList,
                                                    string xmlnsRequested )
        {
            List<ClrNamespaceAssemblyPair> pairList = new List<ClrNamespaceAssemblyPair>();

            // For each assembly, enmerate all the XmlnsDefinition attributes.
            for(int asmIdx=0; asmIdx<asmList.Length; asmIdx++)
            {
                string assemblyName = asmList[asmIdx].FullName;
                Attribute[] attributes = GetAttributes(asmList[asmIdx],
                                                typeof(XmlnsDefinitionAttribute));
                for(int attrIdx=0; attrIdx<attributes.Length; attrIdx++)
                {
                    string xmlns = null;
                    string clrns = null;
                    string asmname = null;

                    GetNamespacesFromDefinitionAttr(attributes[attrIdx], out xmlns, out clrns, out asmname);

                    if (String.IsNullOrEmpty(xmlns) || String.IsNullOrEmpty(clrns) )
                    {
                        throw new ArgumentException(SR.Get(SRID.ParserAttributeArgsLow, "XmlnsDefinitionAttribute"));
                    }

                    if (0 == String.CompareOrdinal(xmlnsRequested, xmlns))
                    {
                        pairList.Add(new ClrNamespaceAssemblyPair(clrns, asmname ?? assemblyName));
                    }
                }
            }
            return pairList;
        }
#endif

        private void ProcessXmlnsCompatibleWithAttributes(Assembly[] asmList)
        {
            // For each assembly, enmerate all the XmlnsCompatibleWith attributes.
            for(int asmIdx=0; asmIdx<asmList.Length; asmIdx++)
            {
#if PBTCOMPILER
                CustomAttributeData[] attributes = GetAttributes(asmList[asmIdx],
                                                "System.Windows.Markup.XmlnsCompatibleWithAttribute");
#else
                Attribute[] attributes = GetAttributes(asmList[asmIdx],
                                                typeof(XmlnsCompatibleWithAttribute));
#endif

                for(int attrIdx=0; attrIdx<attributes.Length; attrIdx++)
                {
                    string oldXmlns = null;
                    string newXmlns = null;

                    GetNamespacesFromCompatAttr(attributes[attrIdx], out oldXmlns, out newXmlns);

                    if (String.IsNullOrEmpty(oldXmlns) || String.IsNullOrEmpty(newXmlns))
                    {
                        throw new ArgumentException(SR.Get(SRID.ParserAttributeArgsLow, "XmlnsCompatibleWithAttribute"));
                    }

                    if (_compatTable.ContainsKey(oldXmlns) &&
                        _compatTable[oldXmlns] != newXmlns)
                    {
                        throw new InvalidOperationException(SR.Get(SRID.ParserCompatDuplicate, oldXmlns,
                                                                   _compatTable[oldXmlns]));
                    }
                    _compatTable[oldXmlns] = newXmlns;
                    _compatTableReverse[newXmlns] = oldXmlns;
                }
            }
        }

        // Table where Xml namespace is the key, and value is a list of assembly / clrns pairs
        private HybridDictionary _cacheTable = null;

        // Table where old namespaces are the key, and new namespaces are the value
        private Dictionary<string, string> _compatTable = null;

        // Table where new namespaces are the key, and old namespaces are the value
        private Dictionary<string, string> _compatTableReverse = null;

#if !PBTCOMPILER
        private HybridDictionary _uriToAssemblyNameTable = null;
#endif

    }

    // Structure used to associate a Clr namespace with the assembly that contains it.
    internal struct ClrNamespaceAssemblyPair
    {
        // constructor
        internal ClrNamespaceAssemblyPair(string clrNamespace,string assemblyName)
        {
            _clrNamespace = clrNamespace;
            _assemblyName = assemblyName;
#if PBTCOMPILER
            _localAssembly = false;
#endif
        }

        // AssemblyName specified in the using Data
        internal string AssemblyName
        {
            get { return _assemblyName; }
        }

        // ClrNamespace portion of the using syntax
        internal string ClrNamespace
        {
            get { return _clrNamespace; }
        }

#if PBTCOMPILER

        internal bool LocalAssembly
        {
            get { return _localAssembly; }
            set { _localAssembly = value; }
        }
#endif

#if PBTCOMPILER
        bool _localAssembly;
#endif
        string _assemblyName;
        string _clrNamespace;
    }
}
