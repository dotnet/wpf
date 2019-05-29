// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MS.Internal;

namespace System
{
    // There are cases where we have multiple assemblies that are going to import this file and
    // if they are going to also have InternalsVisibleTo between them, there will be a compiler warning
    // that the type is found both in the source and in a referenced assembly. The compiler will prefer
    // the version of the type defined in the source
    //
    // In order to disable the warning for this type we are disabling this warning for this entire file.
    #pragma warning disable 436

    internal static partial class AppContextDefaultValues
    {
        static partial void PopulateDefaultValuesPartial(string platformIdentifier, string profile, int targetFrameworkVersion)
        {
            // The AppContext  analyzer expects an if statement here, we should have named the switch 'DoNotUseAdorner' and not included this line at all - by default, switches get set to 'false'
            // Because this was realized after we shipped, we are going to disable the warning for this switch.
#pragma warning disable BCL0012

            // The standard behavior is to draw Text/PasswordBox selections via the Adorner.
            // We want this to always be the case unless it is explicitly changed, regardless of .NET target version.
            LocalAppContext.DefineSwitchDefault(FrameworkAppContextSwitches.UseAdornerForTextboxSelectionRenderingSwitchName, true);
#pragma warning restore BCL0012

            switch (platformIdentifier)
            {
                case ".NETFramework":
                    {
                        if (targetFrameworkVersion <= 40502)
                        {
                            LocalAppContext.DefineSwitchDefault(FrameworkAppContextSwitches.DoNotApplyLayoutRoundingToMarginsAndBorderThicknessSwitchName, true);
                        }
                        if (targetFrameworkVersion <= 40602)
                        {
                            LocalAppContext.DefineSwitchDefault(FrameworkAppContextSwitches.GridStarDefinitionsCanExceedAvailableSpaceSwitchName, true);
                        }
                        if (targetFrameworkVersion <= 40700)
                        {
                            LocalAppContext.DefineSwitchDefault(FrameworkAppContextSwitches.SelectionPropertiesCanLagBehindSelectionChangedEventSwitchName, true);
                        }
                        if (targetFrameworkVersion <= 40701)
                        {
                            LocalAppContext.DefineSwitchDefault(FrameworkAppContextSwitches.DoNotUseFollowParentWhenBindingToADODataRelationSwitchName, true);
                        }
                        if (40000 <= targetFrameworkVersion && targetFrameworkVersion <= 40702)
                        {
                            LocalAppContext.DefineSwitchDefault(FrameworkAppContextSwitches.IListIndexerHidesCustomIndexerSwitchName, true);
                        }
                    }
                    break;

                case ".NETCoreApp":
                    {
                        InitializeNetFxSwitchDefaultsForNetCoreRuntime();
                    }
                    break;
            }
        }

        private static void InitializeNetFxSwitchDefaultsForNetCoreRuntime()
        {
            LocalAppContext.DefineSwitchDefault(FrameworkAppContextSwitches.DoNotApplyLayoutRoundingToMarginsAndBorderThicknessSwitchName, false);
            LocalAppContext.DefineSwitchDefault(FrameworkAppContextSwitches.GridStarDefinitionsCanExceedAvailableSpaceSwitchName, false);
            LocalAppContext.DefineSwitchDefault(FrameworkAppContextSwitches.SelectionPropertiesCanLagBehindSelectionChangedEventSwitchName, false);
            LocalAppContext.DefineSwitchDefault(FrameworkAppContextSwitches.DoNotUseFollowParentWhenBindingToADODataRelationSwitchName, false);
            LocalAppContext.DefineSwitchDefault(FrameworkAppContextSwitches.IListIndexerHidesCustomIndexerSwitchName, false);

            LocalAppContext.DefineSwitchDefault(FrameworkAppContextSwitches.AppendLocalAssemblyVersionForSourceUriSwitchName, false);
            LocalAppContext.DefineSwitchDefault(FrameworkAppContextSwitches.KeyboardNavigationFromHyperlinkInItemsControlIsNotRelativeToFocusedElementSwitchName, false);
            LocalAppContext.DefineSwitchDefault(FrameworkAppContextSwitches.ItemAutomationPeerKeepsItsItemAliveSwitchName, false);

            // UseAdornerForTextboxSelectionRenderingSwitchName is always true, i.e., disabled by default. 
            // Do not initialized this again - this was initialized earlier in PopulateDefaultValuesPartial unconditionally.
            // LocalAppContext.DefineSwitchDefault(FrameworkAppContextSwitches.UseAdornerForTextboxSelectionRenderingSwitchName, true);
        }
    }

    #pragma warning restore 436
}
