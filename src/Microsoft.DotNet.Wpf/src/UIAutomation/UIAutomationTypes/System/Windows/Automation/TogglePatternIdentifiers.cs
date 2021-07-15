// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Automation Identifiers for Toggle Pattern

using System;
using MS.Internal.Automation;
using System.Runtime.InteropServices;

namespace System.Windows.Automation
{
    /// <summary>
    /// The set of states a Toggleable control can be in.
    /// </summary>
    [ComVisible(true)]
    [Guid("ad7db4af-7166-4478-a402-ad5b77eab2fa")]
#if (NO_INTERNAL_COMPILE_BUG1080665)
    internal enum ToggleState
#else
    public enum ToggleState
#endif
    {
        /// <summary>Element is Not Activated: unpressed, unchecked, unmarked, etc.</summary>
        Off,
        /// <summary>Element is Activated: depressed, checked, marked, etc.</summary>
        On,
        /// <summary>Element is in indeterminate state: partially checked, etc.</summary>
        Indeterminate
    }

    /// <summary>
    /// Represents UI elements that have a set of states that can by cycled through such as a checkbox, or toggle button.
    /// </summary>
#if (INTERNAL_COMPILE)
    internal static class TogglePatternIdentifiers
#else
    public static class TogglePatternIdentifiers
#endif
    {
        //------------------------------------------------------
        //
        //  Public Constants / Readonly Fields
        //
        //------------------------------------------------------
 
        #region Public Constants and Readonly Fields

        /// <summary>Toggle pattern</summary>
        public static readonly AutomationPattern Pattern = AutomationPattern.Register(AutomationIdentifierConstants.Patterns.Toggle, "TogglePatternIdentifiers.Pattern");

        /// <summary>Property ID: ToggleState - Value of a toggleable control, as a ToggleState enum</summary>
        public static readonly AutomationProperty ToggleStateProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.ToggleToggleState, "TogglePatternIdentifiers.ToggleStateProperty");

        #endregion Public Constants and Readonly Fields
    }
}
