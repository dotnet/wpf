// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Automation Identifiers for Value Pattern

using System;
using MS.Internal.Automation;

namespace System.Windows.Automation
{
    /// <summary>
    /// Represents UI elements that are expressing a value
    /// </summary>
#if (INTERNAL_COMPILE)
    internal static class ValuePatternIdentifiers
#else
    public static class ValuePatternIdentifiers
#endif
    {
        //------------------------------------------------------
        //
        //  Public Constants / Readonly Fields
        //
        //------------------------------------------------------
 
        #region Public Constants and Readonly Fields

        /// <summary>Value pattern</summary>
        public static readonly AutomationPattern Pattern = AutomationPattern.Register(AutomationIdentifierConstants.Patterns.Value, "ValuePatternIdentifiers.Pattern");

        /// <summary>Property ID: Value - Value of a value control, as a human-readable string</summary>
        public static readonly AutomationProperty ValueProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.ValueValue, "ValuePatternIdentifiers.ValueProperty");

        /// <summary>Property ID: IsReadOnly - Indicates that the value can only be read, not modified.</summary>
        public static readonly AutomationProperty IsReadOnlyProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.ValueIsReadOnly, "ValuePatternIdentifiers.IsReadOnlyProperty");

        #endregion Public Constants and Readonly Fields
    }
}
