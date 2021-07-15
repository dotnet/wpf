// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: services for code that potentially loads uncommon assemblies.
//

/*
    Most of the WPF codebase uses types from WPF's own assemblies or from certain
    standard .Net assemblies (System, mscorlib, etc.).   However, some code uses
    types from other assemblies (System.Xml, System.Data, etc.) - we'll refer to
    these as "uncommon" assemblies.   We don't want WPF to load an uncommon assembly
    unless the app itself needs to.

    The AssemblyHelper class helps to solve this problem by keeping track of which
    uncommon assemblies have been loaded.  Any code that uses an uncommon assembly
    should be isolated in a separate "extension" assembly,
    and calls to that method should be routed through the corresponding extension helper.
    The helper classes check whether the uncommon assembly is loaded before
    loading the extension assembly.
*/

using System;
using System.IO;                    // FileNotFoundException
using System.Reflection;            // Assembly
using System.Security;              // 

using MS.Internal.WindowsBase;      // [FriendAccessAllowed] // BuildInfo

namespace MS.Internal
{
    [FriendAccessAllowed]
    internal enum UncommonAssembly
    {
        // Each enum name must match the assembly name, with dots replaced by underscores
        System_Drawing_Common,
        System_Private_Xml,
        System_Private_Xml_Linq,
        System_Data_Common,
        System_Linq_Expressions,
    }

    [FriendAccessAllowed]
    internal static class AssemblyHelper
    {
        #region Constructors

        static AssemblyHelper()
        {
            // create the records for each uncommon assembly
            string[] names = Enum.GetNames(typeof(UncommonAssembly));
            int n = names.Length;
            _records = new AssemblyRecord[n];

            for (int i=0; i<n; ++i)
            {
                _records[i].Name = names[i].Replace('_','.') + ",";  // comma delimits simple name within Assembly.FullName
            }

            // register for AssemblyLoad event
            AppDomain domain = AppDomain.CurrentDomain;
            domain.AssemblyLoad += OnAssemblyLoad;

            // handle the assemblies that are already loaded
            Assembly[] assemblies = domain.GetAssemblies();
            for (int i=assemblies.Length-1;  i>=0;  --i)
            {
                OnLoaded(assemblies[i]);
            }
        }

        #endregion Constructors

        #region Internal Methods

        [FriendAccessAllowed]
        internal static bool IsLoaded(UncommonAssembly assemblyEnum)
        {
            // this method is typically called by WPF code on a UI thread.
            // Although assemblies can load on any thread, there's no need to lock.
            // If the object of interest came from the given assembly, the
            // AssemblyLoad event has already been raised and the bit has already
            // been set before the caller calls IsLoaded.
            return _records[(int)assemblyEnum].IsLoaded;
        }

        #endregion Internal Methods

        #region System.Drawing

        static SystemDrawingExtensionMethods _systemDrawingExtensionMethods;

        // load the extension class for System.Drawing
        internal static SystemDrawingExtensionMethods ExtensionsForSystemDrawing(bool force=false)
        {
            if (_systemDrawingExtensionMethods == null &&
                (force || IsLoaded(UncommonAssembly.System_Drawing_Common)))
            {
                _systemDrawingExtensionMethods = (SystemDrawingExtensionMethods)LoadExtensionFor("SystemDrawing");
            }

            return _systemDrawingExtensionMethods;
        }

        #endregion System.Drawing

        #region System.Xml

        static SystemXmlExtensionMethods _systemXmlExtensionMethods;

        // load the extension class for System.Xml
        internal static SystemXmlExtensionMethods ExtensionsForSystemXml(bool force=false)
        {
            if (_systemXmlExtensionMethods == null &&
                (force || IsLoaded(UncommonAssembly.System_Private_Xml)))
            {
                _systemXmlExtensionMethods = (SystemXmlExtensionMethods)LoadExtensionFor("SystemXml");
            }

            return _systemXmlExtensionMethods;
        }

        #endregion System.Xml

        #region System.Xml.Linq

        static SystemXmlLinqExtensionMethods _systemXmlLinqExtensionMethods;

        // load the extension class for System.XmlLinq
        internal static SystemXmlLinqExtensionMethods ExtensionsForSystemXmlLinq(bool force=false)
        {
            if (_systemXmlLinqExtensionMethods == null &&
                (force || IsLoaded(UncommonAssembly.System_Private_Xml_Linq)))
            {
                _systemXmlLinqExtensionMethods = (SystemXmlLinqExtensionMethods)LoadExtensionFor("SystemXmlLinq");
            }

            return _systemXmlLinqExtensionMethods;
        }

        #endregion System.Xml.Linq

        #region System.Data

        static SystemDataExtensionMethods _systemDataExtensionMethods;

        // load the extension class for System.Data
        internal static SystemDataExtensionMethods ExtensionsForSystemData(bool force=false)
        {
            if (_systemDataExtensionMethods == null &&
                (force || IsLoaded(UncommonAssembly.System_Data_Common)))
            {
                _systemDataExtensionMethods = (SystemDataExtensionMethods)LoadExtensionFor("SystemData");
            }

            return _systemDataExtensionMethods;
        }

        #endregion System.Data

        #region System.Core

        static SystemCoreExtensionMethods _systemCoreExtensionMethods;

        // load the extension class for System.Core
        internal static SystemCoreExtensionMethods ExtensionsForSystemCore(bool force=false)
        {
            // System.Core is always loaded by default on .NET Framework. On .NET Core, 
            // System.Core is a facade assembly never loaded by the CLR, and System.Core types 
            // have been forwarded to System.Runtime (always loaded by default) and 
            // System.Linq.Expressions.  Only load the extension assembly if System.Linq.Expressions 
            // has already been loaded (e.g., a System.Dynamic type has been created).  
            if (_systemCoreExtensionMethods == null &&
                (force || IsLoaded(UncommonAssembly.System_Linq_Expressions)))
            {
                _systemCoreExtensionMethods = (SystemCoreExtensionMethods)LoadExtensionFor("SystemCore");
            }

            return _systemCoreExtensionMethods;
        }

        #endregion System.Core

        #region Private Methods

        // Get the extension class for the given assembly
        private static object LoadExtensionFor(string name)
        {
            // build the full display name of the extension assembly
            string assemblyName = Assembly.GetExecutingAssembly().FullName;
            string extensionAssemblyName = assemblyName.Replace("WindowsBase", "PresentationFramework-" + name)
                                                        .Replace(BuildInfo.WCP_PUBLIC_KEY_TOKEN, BuildInfo.DEVDIV_PUBLIC_KEY_TOKEN);
            string extensionTypeName = "MS.Internal." + name + "Extension";
            object result = null;

            // create the instance of the extension class
            Type extensionType = Type.GetType($"{extensionTypeName}, {extensionAssemblyName}", throwOnError:false);
            if (extensionType != null)
            {
                result = Activator.CreateInstance(extensionType);
            }

            return result;
        }

        private static void OnAssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            OnLoaded(args.LoadedAssembly);
        }

        private static void OnLoaded(Assembly assembly)
        {
            // although this method can be called on an arbitrary thread, there's no
            // need to lock.  The only change it makes is a monotonic one - changing
            // a bit in an AssemblyRecord from false to true.  Even if two threads try
            // to do this simultaneously, the same outcome results.

            // ignore reflection-only assemblies - we care about running code from the assembly
            if (assembly.ReflectionOnly)
                return;

            // see if the assembly matches one of the uncommon assemblies
            for (int i=_records.Length-1; i>=0; --i)
            {
                if (!_records[i].IsLoaded &&
                    assembly.FullName.StartsWith(_records[i].Name, StringComparison.OrdinalIgnoreCase))
                {
                    _records[i].IsLoaded = true;
                }
            }
        }

        #endregion Private Methods

        #region Private Data

        private struct AssemblyRecord
        {
            public string Name { get; set; }
            public bool IsLoaded { get; set; }
        }

        private static AssemblyRecord[] _records;

        #endregion Private Data
    }
}
