// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Automation Identifiers for SelectionItem Pattern

using System;
using System.Collections;
using MS.Internal.Automation;

namespace System.Windows.Automation
{
    /// <summary>
    /// Class representing containers that manage selection.
    /// </summary>
#if (INTERNAL_COMPILE)
    internal static class SelectionItemPatternIdentifiers
#else
    public static class SelectionItemPatternIdentifiers
#endif
    {
        //------------------------------------------------------
        //
        //  Public Constants / Readonly Fields
        //
        //------------------------------------------------------
 
        #region Public Constants and Readonly Fields

        /// <summary>SelectionItem pattern</summary>
        public static readonly AutomationPattern Pattern = AutomationPattern.Register(AutomationIdentifierConstants.Patterns.SelectionItem, "SelectionItemPatternIdentifiers.Pattern");

        /// <summary>Indicates the element is currently selected.</summary>
        public static readonly AutomationProperty IsSelectedProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.SelectionItemIsSelected, "SelectionItemPatternIdentifiers.IsSelectedProperty");
        /// <summary>Indicates the element is currently selected.</summary>
        public static readonly AutomationProperty SelectionContainerProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.SelectionItemSelectionContainer, "SelectionItemPatternIdentifiers.SelectionContainerProperty");

        /// <summary>
        /// Event ID: ElementAddedToSelection - indicates an element was added to the selection.
        /// sourceElement  refers to the element that was added to the selection.
        /// </summary>
        public static readonly AutomationEvent ElementAddedToSelectionEvent = AutomationEvent.Register(AutomationIdentifierConstants.Events.SelectionItem_ElementAddedToSelection, "SelectionItemPatternIdentifiers.ElementAddedToSelectionEvent");
        /// <summary>
        /// Event ID: ElementRemovedFromSelection - indicates an element was removed from the selection.
        /// sourceElement refers to the element that was removed from the selection.
        /// </summary>
        public static readonly AutomationEvent ElementRemovedFromSelectionEvent = AutomationEvent.Register(AutomationIdentifierConstants.Events.SelectionItem_ElementRemovedFromSelection, "SelectionItemPatternIdentifiers.ElementRemovedFromSelectionEvent");
        /// <summary>
        /// Event ID: ElementSelected - indicates an element was selected in a selection container, deselecting
        /// any previously selected elements in that container.
        /// sourceElement refers to the selected element
        /// </summary>
        public static readonly AutomationEvent ElementSelectedEvent = AutomationEvent.Register(AutomationIdentifierConstants.Events.SelectionItem_ElementSelected, "SelectionItemPatternIdentifiers.ElementSelectedEvent");

        #endregion Public Constants and Readonly Fields
    }
}
