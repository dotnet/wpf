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
using System.Security;              // [SecurityCritical]
using System.Security.Permissions;  // [ReflectionPermission]

using MS.Internal.WindowsBase;      // [FriendAccessAllowed] // BuildInfo

namespace MS.Internal
{
    [FriendAccessAllowed]
    internal enum UncommonAssembly
    {
        // Each enum name must match the assembly name, with dots replaced by underscores
        System_Drawing,
        System_Xml,
        System_Private_Xml,
        System_Xml_Linq,
        System_Private_Xml_Linq,
        System_Data,
        System_Core,
    }

    [FriendAccessAllowed]
    internal static class AssemblyHelper
    {
        #region Constructors

        /// <SecurityNote>
        ///     Critical: accesses AppDomain.AssemblyLoad event
        ///     TreatAsSafe: the event is not exposed - merely updates internal state.
        /// </SecurityNote>
        [SecurityCritical,SecurityTreatAsSafe]
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

        /// <SecurityNote>
        ///     Critical: accesses critical field _records
        ///     TreatAsSafe: it's OK to read the IsLoaded bit
        /// </SecurityNote>
        [SecurityCritical,SecurityTreatAsSafe]
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
                (force || IsLoaded(UncommonAssembly.System_Drawing)))
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
                (force || IsLoaded(UncommonAssembly.System_Xml) || IsLoaded(UncommonAssembly.System_Private_Xml)))
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
                (force || IsLoaded(UncommonAssembly.System_Xml_Linq) || IsLoaded(UncommonAssembly.System_Private_Xml_Linq)))
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
                (force || IsLoaded(UncommonAssembly.System_Data)))
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
            if (_systemCoreExtensionMethods == null &&
                (force || IsLoaded(UncommonAssembly.System_Core)))
            {
                _systemCoreExtensionMethods = (SystemCoreExtensionMethods)LoadExtensionFor("SystemCore");
            }

            return _systemCoreExtensionMethods;
        }

        #endregion System.Core

        #region Private Methods

        // Get the extension class for the given assembly
        /// <SecurityNote>
        ///     Critical:  Asserts RestrictedMemberAccess permission
        ///     TreatAsSafe:  Only used internally to load our own types
        /// </SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        [ReflectionPermission(SecurityAction.Assert, RestrictedMemberAccess=true)]
        private static object LoadExtensionFor(string name)
        {
            // The docs claim that Activator.CreateInstance will create an instance
            // of an internal type provided that (a) the caller has ReflectionPermission
            // with the RestrictedMemberAccess flag, and (b) the grant set of the
            // calling assembly (WindowsBase) is a superset of the grant set of the
            // target assembly (one of our extension assemblies).   Both those conditions
            // are satisfied, yet the call still results in a security exception when run
            // under partial trust (specifically, in the PT environment created by
            // the WPF test infrastructure).   The only workaround I've found is to
            // assert full trust.
            PermissionSet ps = new PermissionSet(PermissionState.Unrestricted);
            ps.Assert();

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

        /// <SecurityNote>
        ///     Critical:  This code potentially sets the IsLoaded bit for the given assembly.
        /// </SecurityNote>
        [SecurityCritical]
        private static void OnAssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            OnLoaded(args.LoadedAssembly);
        }

        /// <SecurityNote>
        ///     Critical:  This code potentially sets the IsLoaded bit for the given assembly.
        /// </SecurityNote>
        [SecurityCritical]
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

        /// <SecurityNote>
        ///     Critical:   The IsLoaded status could be used in security-critical
        ///                 situations.  Make sure the IsLoaded bit is only set by authorized
        ///                 code, namely OnLoaded.
        /// </SecurityNote>
        [SecurityCritical]
        private static AssemblyRecord[] _records;

        #endregion Private Data
    }
}
