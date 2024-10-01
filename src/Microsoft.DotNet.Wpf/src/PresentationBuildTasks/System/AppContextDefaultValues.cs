// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MS.Internal;

namespace System
{
    internal static partial class AppContextDefaultValues
    {
        /// <summary>
        /// This is a partial method. This method is responsible for populating the default values based on a TFM.
        /// It is partial because each library should define this method in their code to contain their defaults.
        /// </summary> 
        static partial void PopulateDefaultValuesPartial(string platformIdentifier, string profile, int targetFrameworkVersion)
        {
            switch (platformIdentifier)
            {
                case ".NETFramework":
                    if (targetFrameworkVersion <= 40701)
                    {
                        LocalAppContext.DefineSwitchDefault(BuildTasksAppContextSwitches.DoNotUseSha256ForMarkupCompilerChecksumAlgorithmSwitchName, true);
                    }
                    break;

                case ".NETCoreApp":
                    LocalAppContext.DefineSwitchDefault(BuildTasksAppContextSwitches.DoNotUseSha256ForMarkupCompilerChecksumAlgorithmSwitchName, false);
                    break;
            }
        }
    }
}
