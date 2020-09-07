// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Automation Identifiers for Selection Pattern

using System;
using System.Collections;
using System.ComponentModel;
using MS.Internal.Automation;

namespace System.Windows.Automation
{
    /// <summary>
    /// Class representing containers that manage selection.
    /// </summary>
#if (INTERNAL_COMPILE)
    internal static class SelectionPatternIdentifiers
#else
    public static class SelectionPatternIdentifiers
#endif
    {
        //------------------------------------------------------
        //
        //  Public Constants / Readonly Fields
        //
        //------------------------------------------------------
 
        #region Public Constants and Readonly Fields

        /// <summary>Selection pattern</summary>
        public static readonly AutomationPattern Pattern = AutomationPattern.Register(AutomationIdentifierConstants.Patterns.Selection, "SelectionPatternIdentifiers.Pattern");

        /// <summary>Get the currently selected elements</summary>
        public static readonly AutomationProperty SelectionProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.SelectionSelection, "SelectionPatternIdentifiers.SelectionProperty");

        /// <summary>Indicates whether the control allows more than one element to be selected</summary>
        public static readonly AutomationProperty CanSelectMultipleProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.SelectionCanSelectMultiple, "SelectionPatternIdentifiers.CanSelectMultipleProperty");

        /// <summary>Indicates whether the control requires at least one element to be selected</summary>
        public static readonly AutomationProperty IsSelectionRequiredProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.SelectionIsSelectionRequired, "SelectionPatternIdentifiers.IsSelectionRequiredProperty");

        /// <summary>
        /// Event ID: SelectionInvalidated - indicates that selection changed in a selection conainer.
        /// sourceElement refers to the selection container
        /// </summary>
        public static readonly AutomationEvent InvalidatedEvent = AutomationEvent.Register(AutomationIdentifierConstants.Events.Selection_Invalidated, "SelectionPatternIdentifiers.InvalidatedEvent");

        #endregion Public Constants and Readonly Fields
    }
}
