// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MS.Internal;
using System.Windows;

namespace System
{
    // There are cases where we have multiple assemblies that are going to import this file and 
    // if they are going to also have InternalsVisibleTo between them, there will be a compiler warning
    // that the type is found both in the source and in a referenced assembly. The compiler will prefer 
    // the version of the type defined in the source
    //
    // In order to disable the warning for this type we are disabling this warning for this entire file.
    #pragma warning disable 436 

    /// <summary>
    /// Default values for app-compat quirks used within WindowsBase.
    /// Also see BaseAppContextSwitches
    /// </summary>
    internal static partial class AppContextDefaultValues
    {
        static partial void PopulateDefaultValuesPartial(string platformIdentifier, string profile, int targetFrameworkVersion)
        {
            switch (platformIdentifier)
            {
                case ".NETFramework":
                    {
                        if (targetFrameworkVersion <= 40502)
                        {
                            LocalAppContext.DefineSwitchDefault(BaseAppContextSwitches.SwitchDoNotUseCulturePreservingDispatcherOperations, true);
                        }

                        if (targetFrameworkVersion <= 40700)
                        {
                            LocalAppContext.DefineSwitchDefault(BaseAppContextSwitches.SwitchUseSha1AsDefaultHashAlgorithmForDigitalSignatures, true);
                        }
                    }
                    break;

                case ".NETCoreApp":
                    {
                        InitializeNetFxSwitchDefaultsForNetCoreRuntime();
                    }
                    break;
            }

            // Ensure we set all the accessibility switch defaults
            AccessibilitySwitches.SetSwitchDefaults(platformIdentifier, targetFrameworkVersion);
        }

        /// <summary>
        /// This is the full set of .NET Framework <see cref="AppContext"/> in <see cref="BaseAppContextSwitches"/>. These are being initialized
        /// to <code>false</code> to ensure that the corresponding functionality will be treated as if it is enabled by default on .NET Core.
        /// </summary>
        private static void InitializeNetFxSwitchDefaultsForNetCoreRuntime()
        {
            LocalAppContext.DefineSwitchDefault(BaseAppContextSwitches.SwitchDoNotUseCulturePreservingDispatcherOperations, false);
            LocalAppContext.DefineSwitchDefault(BaseAppContextSwitches.SwitchUseSha1AsDefaultHashAlgorithmForDigitalSignatures, false);

            LocalAppContext.DefineSwitchDefault(BaseAppContextSwitches.SwitchDoNotInvokeInWeakEventTableShutdownListener, false);
            LocalAppContext.DefineSwitchDefault(BaseAppContextSwitches.SwitchEnableCleanupSchedulingImprovements, false);
            LocalAppContext.DefineSwitchDefault(BaseAppContextSwitches.SwitchEnableWeakEventMemoryImprovements, false);
        }
    }

    #pragma warning restore 436
}
