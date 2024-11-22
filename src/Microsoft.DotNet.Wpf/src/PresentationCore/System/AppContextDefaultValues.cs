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
            LocalAppContext.DefineSwitchDefault(CoreAppContextSwitches.DoNotScaleForDpiChangesSwitchName, false);
            LocalAppContext.DefineSwitchDefault(CoreAppContextSwitches.OverrideExceptionWithNullReferenceExceptionName, false);
            LocalAppContext.DefineSwitchDefault(CoreAppContextSwitches.DoNotUsePresentationDpiCapabilityTier2OrGreaterSwitchName, false);

            LocalAppContext.DefineSwitchDefault(CoreAppContextSwitches.DisableStylusAndTouchSupportSwitchName, false);
            LocalAppContext.DefineSwitchDefault(CoreAppContextSwitches.EnablePointerSupportSwitchName, false);
            LocalAppContext.DefineSwitchDefault(CoreAppContextSwitches.DisableDiagnosticsSwitchName, false);
            LocalAppContext.DefineSwitchDefault(CoreAppContextSwitches.AllowChangesDuringVisualTreeChangedSwitchName, false);
            LocalAppContext.DefineSwitchDefault(CoreAppContextSwitches.DisableImplicitTouchKeyboardInvocationSwitchName, false);
            LocalAppContext.DefineSwitchDefault(CoreAppContextSwitches.ShouldRenderEvenWhenNoDisplayDevicesAreAvailableSwitchName, false);
            LocalAppContext.DefineSwitchDefault(CoreAppContextSwitches.ShouldNotRenderInNonInteractiveWindowStationSwitchName, false);
            LocalAppContext.DefineSwitchDefault(CoreAppContextSwitches.DoNotUsePresentationDpiCapabilityTier3OrGreaterSwitchName, false);
            LocalAppContext.DefineSwitchDefault(CoreAppContextSwitches.AllowExternalProcessToBlockAccessToTemporaryFilesSwitchName, false);
            LocalAppContext.DefineSwitchDefault(CoreAppContextSwitches.EnableHardwareAccelerationInRdpSwitchName, false);
            LocalAppContext.DefineSwitchDefault(CoreAppContextSwitches.DisableSpecialCharacterLigatureSwitchName, false);
        }
    }
}
