// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System;

// When building PresentationFramework, 'LocalAppContext' from WindowsBase.dll conflicts
// with 'LocalAppContext' from PresentationCore.dll since there is InternalsVisibleTo set
#pragma warning disable CS0436 // Type conflicts with imported type

namespace MS.Internal
{
    internal static class FrameworkAppContextSwitches
    {
        internal const string DoNotApplyLayoutRoundingToMarginsAndBorderThicknessSwitchName = "Switch.MS.Internal.DoNotApplyLayoutRoundingToMarginsAndBorderThickness";
        private static int _doNotApplyLayoutRoundingToMarginsAndBorderThickness;
        public static bool DoNotApplyLayoutRoundingToMarginsAndBorderThickness
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return LocalAppContext.GetCachedSwitchValue(DoNotApplyLayoutRoundingToMarginsAndBorderThicknessSwitchName, ref _doNotApplyLayoutRoundingToMarginsAndBorderThickness);
            }
        }

        internal const string GridStarDefinitionsCanExceedAvailableSpaceSwitchName = "Switch.System.Windows.Controls.Grid.StarDefinitionsCanExceedAvailableSpace";
        private static int _gridStarDefinitionsCanExceedAvailableSpace;
        public static bool GridStarDefinitionsCanExceedAvailableSpace
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return LocalAppContext.GetCachedSwitchValue(GridStarDefinitionsCanExceedAvailableSpaceSwitchName, ref _gridStarDefinitionsCanExceedAvailableSpace);
            }
        }

        internal const string SelectionPropertiesCanLagBehindSelectionChangedEventSwitchName = "Switch.System.Windows.Controls.TabControl.SelectionPropertiesCanLagBehindSelectionChangedEvent";
        private static int _selectionPropertiesCanLagBehindSelectionChangedEvent;
        public static bool SelectionPropertiesCanLagBehindSelectionChangedEvent
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return LocalAppContext.GetCachedSwitchValue(SelectionPropertiesCanLagBehindSelectionChangedEventSwitchName, ref _selectionPropertiesCanLagBehindSelectionChangedEvent);
            }
        }

        internal const string DoNotUseFollowParentWhenBindingToADODataRelationSwitchName = "Switch.System.Windows.Data.DoNotUseFollowParentWhenBindingToADODataRelation";
        private static int _doNotUseFollowParentWhenBindingToADODataRelation;
        public static bool DoNotUseFollowParentWhenBindingToADODataRelation
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return LocalAppContext.GetCachedSwitchValue(DoNotUseFollowParentWhenBindingToADODataRelationSwitchName, ref _doNotUseFollowParentWhenBindingToADODataRelation);
            }
        }

        
        // Switch to enable non-adorner based rendering of TextSelection in TextBox and PasswordBox.
        internal const string UseAdornerForTextboxSelectionRenderingSwitchName = "Switch.System.Windows.Controls.Text.UseAdornerForTextboxSelectionRendering";
        private static int _useAdornerForTextboxSelectionRendering;
        public static bool UseAdornerForTextboxSelectionRendering
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return LocalAppContext.GetCachedSwitchValue(UseAdornerForTextboxSelectionRenderingSwitchName, ref _useAdornerForTextboxSelectionRendering);
            }
        }

        
        // Switch to enable appending the local assembly version to the Uri being set for ResourceDictionary.Source via Baml2006ReaderInternal.
        internal const string AppendLocalAssemblyVersionForSourceUriSwitchName = "Switch.System.Windows.Baml2006.AppendLocalAssemblyVersionForSourceUri";
        private static int _AppendLocalAssemblyVersionForSourceUriSwitchName;
        public static bool AppendLocalAssemblyVersionForSourceUri
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return LocalAppContext.GetCachedSwitchValue(AppendLocalAssemblyVersionForSourceUriSwitchName, ref _AppendLocalAssemblyVersionForSourceUriSwitchName);
            }
        }

        
        // Switch to enable IList indexer hiding a custom indexer in a binding path
        internal const string IListIndexerHidesCustomIndexerSwitchName = "Switch.System.Windows.Data.Binding.IListIndexerHidesCustomIndexer";
        private static int _IListIndexerHidesCustomIndexer;
        public static bool IListIndexerHidesCustomIndexer
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return LocalAppContext.GetCachedSwitchValue(IListIndexerHidesCustomIndexerSwitchName, ref _IListIndexerHidesCustomIndexer);
            }
        }

        
        // Switch to enable keyboard navigation from a hyperlink to go to the wrong place
        internal const string KeyboardNavigationFromHyperlinkInItemsControlIsNotRelativeToFocusedElementSwitchName = "Switch.System.Windows.Controls.KeyboardNavigationFromHyperlinkInItemsControlIsNotRelativeToFocusedElement";
        private static int _KeyboardNavigationFromHyperlinkInItemsControlIsNotRelativeToFocusedElement;
        public static bool KeyboardNavigationFromHyperlinkInItemsControlIsNotRelativeToFocusedElement
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return LocalAppContext.GetCachedSwitchValue(KeyboardNavigationFromHyperlinkInItemsControlIsNotRelativeToFocusedElementSwitchName, ref _KeyboardNavigationFromHyperlinkInItemsControlIsNotRelativeToFocusedElement);
            }
        }

        
        // Switch to opt-out of the ItemAutomationPeer weak-reference.
        // Setting this to true can avoid NRE crashes, but re-introduces memory leaks
        internal const string ItemAutomationPeerKeepsItsItemAliveSwitchName = "Switch.System.Windows.Automation.Peers.ItemAutomationPeerKeepsItsItemAlive";
        private static int _ItemAutomationPeerKeepsItsItemAlive;
        public static bool ItemAutomationPeerKeepsItsItemAlive
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return LocalAppContext.GetCachedSwitchValue(ItemAutomationPeerKeepsItsItemAliveSwitchName, ref _ItemAutomationPeerKeepsItsItemAlive);
            }
        }
    
    
        // Switch to opt-out Fluent theme Window Backdrop feature
        internal const string DisableFluentThemeWindowBackdropSwitchName = "Switch.System.Windows.Appearance.DisableFluentThemeWindowBackdrop";
        private static int _DisableFluentThemeWindowBackdrop;
        public static bool DisableFluentThemeWindowBackdrop
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return LocalAppContext.GetCachedSwitchValue(DisableFluentThemeWindowBackdropSwitchName, ref _DisableFluentThemeWindowBackdrop);
            }
        }


        // Switch to disable DynamicResource optimizations
        internal const string DisableDynamicResourceOptimizationSwitchName = "Switch.System.Windows.Controls.DisableDynamicResourceOptimization";
        private static int _DisableDynamicResourceOptimization;
        public static bool DisableDynamicResourceOptimization
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return LocalAppContext.GetCachedSwitchValue(DisableDynamicResourceOptimizationSwitchName, ref _DisableDynamicResourceOptimization);
            }
        }
    }
}

#pragma warning restore CS0436 // Type conflicts with imported type
