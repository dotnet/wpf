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
using System.Security.Permissions;
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
        /// <SecurityNote>
        ///     Critical: This code sets the allowed assemblies on AssemblyList 
        ///     TreatAsSafe: Initializing the data is ok since it does not expose anything
        /// </SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        static AssemblyFilter()
        {
            _disallowedListExtracted = new SecurityCriticalDataForSet<bool>(false);
            _assemblyList = new SecurityCriticalDataForSet<System.Collections.Generic.List<string>>(new System.Collections.Generic.List<string>());
        }

        /// <SecurityNote>
        ///     Critical: This code calls into unmanaged Api that has a SUC on this (IAssemblCache related)
        /// </SecurityNote>
        [SecurityCritical]
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
                        object[] aptca = a.GetCustomAttributes(typeof(AllowPartiallyTrustedCallersAttribute), false);
                        // if the dll has APTCA
                        if (aptca.Length > 0 && aptca[0] is AllowPartiallyTrustedCallersAttribute)
                        {
                            string assemblyName = AssemblyNameWithFileVersion(a);
                            // If we are on the disallowed list kill the application domain
                            if (AssemblyOnDisallowedList(assemblyName))
                            {
                                // Kill the application domain
                                UnsafeNativeMethods.ProcessUnhandledException_DLL(SR.Get(SRID.KillBitEnforcedShutdown) + assemblyName);
                                // I want to ensure that the process really dies
                                new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Assert();//BlessedAssert
                                try
                                {
                                    System.Environment.Exit(-1);
                                }
                                finally
                                {
                                    SecurityPermission.RevertAssert();
                                    Debug.Fail("Environment.Exit() failed.");
                                }
                            }
                        }
                    }
                }
            }
        }

        //appends assembly name with file version to generate a unique entry for the assembly lookup process
        /// <SecurityNote>
        ///     Critical: This code elevates to extract assembly name
        /// </SecurityNote>
        [SecurityCritical]
        private string AssemblyNameWithFileVersion(Assembly a)
        {
            FileVersionInfo fileVersionInfo;
            StringBuilder sb = new StringBuilder(a.FullName);
            // we need unrestricted here because the location is demands too.
            (new FileIOPermission(PermissionState.Unrestricted)).Assert();//BlessedAssert
            try
            {
                fileVersionInfo = FileVersionInfo.GetVersionInfo(a.Location);
            }
            finally
            {
                FileIOPermission.RevertAssert();
            }
            if (fileVersionInfo != null && fileVersionInfo.ProductVersion != null)
            {
                sb.Append(FILEVERSION_STRING + fileVersionInfo.ProductVersion);
            }
            return ((sb.ToString()).ToLower(System.Globalization.CultureInfo.InvariantCulture)).Trim();
        }

        /// <SecurityNote>
        ///     Critical: This code populates _assemblyList with Disallowed Elements and sets the bit that dictates whether to repopulate it
        /// </SecurityNote>
        [SecurityCritical]
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

        /// <SecurityNote>
        ///     Critical: This code opens an HKLM registry location and reads it. We do not want 
        ///     to call this over and over as it could cause performance issues
        /// </SecurityNote>
        [SecurityCritical]
        private void ExtractDisallowedRegistryList()
        {
            string[] disallowedAssemblies;
            RegistryKey featureKey;
            //Assert for read access to HKLM\Software\Microsoft\.NetFramework\Policy\APTCA
            (new RegistryPermission(RegistryPermissionAccess.Read, KILL_BIT_REGISTRY_HIVE + KILL_BIT_REGISTRY_LOCATION)).Assert();//BlessedAssert
            try
            {
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
            finally
            {
                RegistryPermission.RevertAssert();
            }
        }

        /// <SecurityNote>
        ///     Critical: This holds a list of assemblies that are on an allowed and disallowed list and can be exploited to load
        ///     unsafe dll's into appdomain
        /// </SecurityNote>
        static SecurityCriticalDataForSet<System.Collections.Generic.List<string>> _assemblyList;

        /// <SecurityNote>
        ///     Critical: This bit determines whether we need to hit the registry and load the disallowed elements.
        ///     We would like to see this happen only once per appdomain and delay it as much as possible
        /// </SecurityNote>
        static SecurityCriticalDataForSet<bool> _disallowedListExtracted;

        static object _lock = new object();

        private const string FILEVERSION_STRING = @", FileVersion=";
        // This is the location in the registry where all the keys are stored
        private const string KILL_BIT_REGISTRY_HIVE = @"HKEY_LOCAL_MACHINE\";
        private const string KILL_BIT_REGISTRY_LOCATION = @"Software\Microsoft\.NetFramework\policy\APTCA";
        private const string SUBKEY_VALUE = @"APTCA_FLAG";
    }
}
