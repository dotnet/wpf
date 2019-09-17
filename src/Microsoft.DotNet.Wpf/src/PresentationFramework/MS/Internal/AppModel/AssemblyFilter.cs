// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 
// Description:
//              This class is used to filter assemblies as they are loaded into an application domain.
//              The intent is to bring the AppDomain down in the case that one of these is on a disallowed list
//              similar to the kill bit for Activex
//

using System;
using System.Windows;
using MS.Internal.PresentationFramework;
using System.Collections.Generic;
using MS.Win32;
using Microsoft.Win32;
using System.Security;
using System.Reflection;
using System.Text;
using MS.Internal.AppModel;
using MS.Internal;
using System.Windows.Resources;
using System.Diagnostics;
using System.Runtime.InteropServices;


namespace MS.Internal
{
    internal class AssemblyFilter
    {
        static AssemblyFilter()
        {
            _disallowedListExtracted = new SecurityCriticalDataForSet<bool>(false);
            _assemblyList = new SecurityCriticalDataForSet<System.Collections.Generic.List<string>>(new System.Collections.Generic.List<string>());
        }

        internal void FilterCallback(Object sender, AssemblyLoadEventArgs args)
        {
            // This code is reentrant
            lock (_lock)
            {
                // Extract assembly
                Assembly a = args.LoadedAssembly;
                // xmlns cache loads assemblies as reflection only and we cannot inspect these using the code below
                // so we ignore also keeping this first is super important because the first time cost is really high
                // other wise also we cannot do any processing on a reflection only assembly aside from reflection based actions
                if (!a.ReflectionOnly)
                {
                    // check if it is in the Gac , this ensures that we eliminate any non GAC assembly which are of no risk
                    if (a.GlobalAssemblyCache)
                    {
                        string assemblyName = AssemblyNameWithFileVersion(a);
                        // If we are on the disallowed list kill the application domain
                        if (AssemblyOnDisallowedList(assemblyName))
                        {
                            // Kill the application domain
                            UnsafeNativeMethods.ProcessUnhandledException_DLL(SR.Get(SRID.KillBitEnforcedShutdown) + assemblyName);
                            // I want to ensure that the process really dies
                            try
                            {
                                System.Environment.Exit(-1);
                            }
                            finally
                            {
                                Debug.Fail("Environment.Exit() failed.");
                            }
                        }
                    }
                }
            }
        }

        //appends assembly name with file version to generate a unique entry for the assembly lookup process
        private string AssemblyNameWithFileVersion(Assembly a)
        {
            FileVersionInfo fileVersionInfo;
            StringBuilder sb = new StringBuilder(a.FullName);

            fileVersionInfo = FileVersionInfo.GetVersionInfo(a.Location);
            if (fileVersionInfo != null && fileVersionInfo.ProductVersion != null)
            {
                sb.Append(FILEVERSION_STRING + fileVersionInfo.ProductVersion);
            }
            return ((sb.ToString()).ToLower(System.Globalization.CultureInfo.InvariantCulture)).Trim();
        }

        private bool AssemblyOnDisallowedList(String assemblyToCheck)
        {
            bool retVal = false;
            // if the list disallowed list is not populated populate it once
            if (_disallowedListExtracted.Value == false)
            {
                // hit the registry one time and read 
                ExtractDisallowedRegistryList();
                _disallowedListExtracted.Value = true;
            }
            if (_assemblyList.Value.Contains(assemblyToCheck))
            {
                retVal = true;
            }
            return retVal;
        }

        private void ExtractDisallowedRegistryList()
        {
            string[] disallowedAssemblies;
            RegistryKey featureKey;

            // open the key and read the value
            featureKey = Registry.LocalMachine.OpenSubKey(KILL_BIT_REGISTRY_LOCATION);
            if (featureKey != null)
            {
                // Enumerate through all keys and populate dictionary
                disallowedAssemblies = featureKey.GetSubKeyNames();
                // iterate over this list and for each extract the APTCA_FLAG value and set it in the 
                // dictionary
                foreach (string assemblyName in disallowedAssemblies)
                {
                    featureKey = Registry.LocalMachine.OpenSubKey(KILL_BIT_REGISTRY_LOCATION + @"\" + assemblyName);
                    object keyValue = featureKey.GetValue(SUBKEY_VALUE);
                    // if there exists a value and it is 1 add to hash table
                    if ((keyValue != null) && (int)(keyValue) == 1)
                    {
                        if (!_assemblyList.Value.Contains(assemblyName))
                        {
                            _assemblyList.Value.Add(assemblyName.ToLower(System.Globalization.CultureInfo.InvariantCulture).Trim());
                        }
                    }
                }
            }
        }

        static SecurityCriticalDataForSet<System.Collections.Generic.List<string>> _assemblyList;

        static SecurityCriticalDataForSet<bool> _disallowedListExtracted;

        static object _lock = new object();

        private const string FILEVERSION_STRING = @", FileVersion=";
        // This is the location in the registry where all the keys are stored
        private const string KILL_BIT_REGISTRY_HIVE = @"HKEY_LOCAL_MACHINE\";
        private const string KILL_BIT_REGISTRY_LOCATION = @"Software\Microsoft\.NetFramework\policy\APTCA";
        private const string SUBKEY_VALUE = @"APTCA_FLAG";
    }
}
