// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Automation Identifiers for ItemContainer Pattern

using System;
using MS.Internal.Automation;
using System.Runtime.InteropServices;

namespace System.Windows.Automation
{
    /// <summary>
    /// Represents Containers that maintains items and support item look up by propety value.
    /// </summary>
#if (INTERNAL_COMPILE)
    internal static class ItemContainerPatternIdentifiers
#else
    public static class ItemContainerPatternIdentifiers
#endif
    {
        //------------------------------------------------------
        //
        //  Public Constants / Readonly Fields
        //
        //------------------------------------------------------
 
        #region Public Constants and Readonly Fields

        /// <summary>ItemContainer pattern</summary>
        public static readonly AutomationPattern Pattern = AutomationPattern.Register(AutomationIdentifierConstants.Patterns.ItemContainer, "ItemContainerPatternIdentifiers.Pattern");

        #endregion Public Constants and Readonly Fields
    }
}

