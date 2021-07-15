// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

﻿
using MS.Internal.PresentationFramework.Interop;
using System;
using System.Runtime.CompilerServices;
using System.Windows;

namespace MS.Internal
{
    // There are cases where we have multiple assemblies that are going to import this file and
    // if they are going to also have InternalsVisibleTo between them, there will be a compiler warning
    // that the type is found both in the source and in a referenced assembly. The compiler will prefer
    // the version of the type defined in the source
    //
    // In order to disable the warning for this type we are disabling this warning for this entire file.
    #pragma warning disable 436

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
    }

#pragma warning restore 436
}
