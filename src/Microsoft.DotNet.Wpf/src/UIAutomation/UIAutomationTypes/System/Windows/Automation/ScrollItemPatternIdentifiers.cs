// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Description: Automation Identifiers for ScrollItem Pattern

using MS.Internal.Automation;

namespace System.Windows.Automation
{
    /// <summary>
    /// Represents UI elements in a scrollable area that can be scrolled to.
    /// </summary>
#if (INTERNAL_COMPILE)
    internal static class ScrollItemPatternIdentifiers
#else
    public static class ScrollItemPatternIdentifiers
#endif
    {
        //------------------------------------------------------
        //
        //  Public Constants / Readonly Fields
        //
        //------------------------------------------------------
 
        #region Public Constants and Readonly Fields

        /// <summary>Scroll pattern</summary>
        public static readonly AutomationPattern Pattern = AutomationPattern.Register(AutomationIdentifierConstants.Patterns.ScrollItem, "ScrollItemPatternIdentifiers.Pattern");

        #endregion Public Constants and Readonly Fields
    }
}
