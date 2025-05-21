// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MS.Internal;
using System.Windows;

namespace System
{
    /// <summary>
    /// Default values for app-compat quirks used within WindowsBase.
    /// Also see BaseAppContextSwitches
    /// </summary>
    internal static partial class AppContextDefaultValues
    {
        /// <summary>
        /// This is a partial method. This method is responsible for populating the default values based on a TFM.
        /// It is partial because each library should define this method in their code to contain their defaults.
        /// </summary>
        /// <remarks>
        /// This is the full set of .NET Framework <see cref="AppContext"/> in <see cref="BaseAppContextSwitches"/>. These are being initialized
        /// to <see langword="false"/> to ensure that the corresponding functionality will be treated as if it is enabled by default on .NET Core.
        /// </remarks>
        static partial void PopulateDefaultValuesPartial(string platformIdentifier, string profile, int targetFrameworkVersion)
        {
            LocalAppContext.DefineSwitchDefault(BaseAppContextSwitches.SwitchDoNotUseCulturePreservingDispatcherOperations, false);
            LocalAppContext.DefineSwitchDefault(BaseAppContextSwitches.SwitchUseSha1AsDefaultHashAlgorithmForDigitalSignatures, false);

            LocalAppContext.DefineSwitchDefault(BaseAppContextSwitches.SwitchDoNotInvokeInWeakEventTableShutdownListener, false);
            LocalAppContext.DefineSwitchDefault(BaseAppContextSwitches.SwitchEnableCleanupSchedulingImprovements, false);
            LocalAppContext.DefineSwitchDefault(BaseAppContextSwitches.SwitchEnableWeakEventMemoryImprovements, false);

            // Ensure we set all the accessibility switch defaults
            AccessibilitySwitches.SetSwitchDefaults(platformIdentifier, targetFrameworkVersion);
        }
    }
}
