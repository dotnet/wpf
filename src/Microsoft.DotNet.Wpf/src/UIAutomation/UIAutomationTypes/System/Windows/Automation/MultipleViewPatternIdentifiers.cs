// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Automation Identifiers for MultipleView Pattern

using System;
using MS.Internal.Automation;

namespace System.Windows.Automation
{
       
    ///<summary>wrapper class for MultipleView pattern </summary>
#if (INTERNAL_COMPILE)
    internal static class MultipleViewPatternIdentifiers
#else
    public static class MultipleViewPatternIdentifiers
#endif
    {
        //------------------------------------------------------
        //
        //  Public Constants / Readonly Fields
        //
        //------------------------------------------------------
 
        #region Public Constants and Readonly Fields

        /// <summary>MultipleView pattern</summary>
        public static readonly AutomationPattern Pattern = AutomationPattern.Register(AutomationIdentifierConstants.Patterns.MultipleView, "MultipleViewPatternIdentifiers.Pattern");

        /// <summary>Property ID: CurrentView - The view ID corresponding to the control's current state. This ID is control-specific.</summary>
        public static readonly AutomationProperty CurrentViewProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.MultipleViewCurrentView, "MultipleViewPatternIdentifiers.CurrentViewProperty");

        /// <summary>Property ID: SupportedViews - Returns an array of ints representing the full set of views available in this control.</summary>
        public static readonly AutomationProperty SupportedViewsProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.MultipleViewSupportedViews, "MultipleViewPatternIdentifiers.SupportedViewsProperty");

        #endregion Public Constants and Readonly Fields
    }
}
