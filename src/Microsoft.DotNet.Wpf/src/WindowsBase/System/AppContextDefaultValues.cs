// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//---------------------------------------------------------------------------
//
// 
// 
// File: AppContextDefaultValues.cs
//---------------------------------------------------------------------------

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

                        // Ensure we set all the accessibility switch defaults
                        AccessibilitySwitches.SetSwitchDefaults(targetFrameworkVersion);

                        break;
                    }
            }
        }
    }

    #pragma warning restore 436
}
