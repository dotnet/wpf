// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Automation Identifiers for VirtualizedItem Pattern

using System;
using MS.Internal.Automation;
using System.Runtime.InteropServices;

namespace System.Windows.Automation
{
    /// <summary>
    /// Represents items inside containers which can be virtualized, this pattern can be used to realize them.
    /// </summary>
#if (INTERNAL_COMPILE)
    internal static class VirtualizedItemPatternIdentifiers
#else
    public static class VirtualizedItemPatternIdentifiers
#endif
    {
        //------------------------------------------------------
        //
        //  Public Constants / Readonly Fields
        //
        //------------------------------------------------------
 
        #region Public Constants and Readonly Fields

        /// <summary>VirtualizedItem pattern</summary>
        public static readonly AutomationPattern Pattern = AutomationPattern.Register(AutomationIdentifierConstants.Patterns.VirtualizedItem, "VirtualizedItemPatternIdentifiers.Pattern");

        #endregion Public Constants and Readonly Fields
    }
}
