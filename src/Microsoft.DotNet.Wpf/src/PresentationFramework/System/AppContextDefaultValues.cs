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
            // When building PresentationFramework, 'LocalAppContext' from WindowsBase.dll conflicts
            // with 'LocalAppContext' from PresentationCore.dll since there is InternalsVisibleTo set
#pragma warning disable CS0436 // Type conflicts with imported type

            // The standard behavior is to draw Text/PasswordBox selections via the Adorner.
            // We want this to always be the case unless it is explicitly changed, regardless of .NET target version.
            LocalAppContext.DefineSwitchDefault(FrameworkAppContextSwitches.UseAdornerForTextboxSelectionRenderingSwitchName, true);

            LocalAppContext.DefineSwitchDefault(FrameworkAppContextSwitches.DoNotApplyLayoutRoundingToMarginsAndBorderThicknessSwitchName, false);
            LocalAppContext.DefineSwitchDefault(FrameworkAppContextSwitches.GridStarDefinitionsCanExceedAvailableSpaceSwitchName, false);
            LocalAppContext.DefineSwitchDefault(FrameworkAppContextSwitches.SelectionPropertiesCanLagBehindSelectionChangedEventSwitchName, false);
            LocalAppContext.DefineSwitchDefault(FrameworkAppContextSwitches.DoNotUseFollowParentWhenBindingToADODataRelationSwitchName, false);
            LocalAppContext.DefineSwitchDefault(FrameworkAppContextSwitches.IListIndexerHidesCustomIndexerSwitchName, false);

            LocalAppContext.DefineSwitchDefault(FrameworkAppContextSwitches.AppendLocalAssemblyVersionForSourceUriSwitchName, false);
            LocalAppContext.DefineSwitchDefault(FrameworkAppContextSwitches.KeyboardNavigationFromHyperlinkInItemsControlIsNotRelativeToFocusedElementSwitchName, false);
            LocalAppContext.DefineSwitchDefault(FrameworkAppContextSwitches.ItemAutomationPeerKeepsItsItemAliveSwitchName, false);
            LocalAppContext.DefineSwitchDefault(FrameworkAppContextSwitches.DisableFluentThemeWindowBackdropSwitchName, false);
            LocalAppContext.DefineSwitchDefault(FrameworkAppContextSwitches.DisableDynamicResourceOptimizationSwitchName, true);

#pragma warning restore CS0436 // Type conflicts with imported type
        }
    }
}
