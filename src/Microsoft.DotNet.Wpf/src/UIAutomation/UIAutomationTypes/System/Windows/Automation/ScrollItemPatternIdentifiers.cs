// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Automation Identifiers for ScrollItem Pattern

using System;
using MS.Internal.Automation;
using System.Runtime.InteropServices;

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
