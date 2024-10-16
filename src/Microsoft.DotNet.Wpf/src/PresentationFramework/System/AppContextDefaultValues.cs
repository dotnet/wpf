// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Since multiple assemblies (WindowsBase, PresentationFramework, PresentationCore, PresentationBuildTasks)
// share this file/class and have InternalsVisibleTo set between them, there will be a compiler warning
// that the type is found both in the source and in a referenced assembly. The compiler will prefer 
// the version of the type defined in the source once the warning is supressed and won't compile the rest.

// In order to disable the warning for this type we are disabling the warning for this entire file.
#pragma warning disable CS0436 // Type conflicts with imported type

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
        }
    }
}

#pragma warning restore CS0436 // Type conflicts with imported type
