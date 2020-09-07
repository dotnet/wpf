// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Automation Identifiers for RangeValue Pattern

using System;
using MS.Internal.Automation;

namespace System.Windows.Automation
{

    /// <summary>
    /// Exposes a related set of properties that reflect a control's ability to manage a value
    /// within a finite range.  It conveys a controls valid minimum and maximum values and its
    /// current value.
    ///
    ///  Pattern requires MinValue less than MaxValue. 
    ///  MinimumValue and MaximumValue must be the same Object type as ValueAsObject.
    /// </summary>
#if (INTERNAL_COMPILE)
    internal static class RangeValuePatternIdentifiers
#else
    public static class RangeValuePatternIdentifiers
#endif
    {
        //------------------------------------------------------
        //
        //  Public Constants / Readonly Fields
        //
        //------------------------------------------------------
 
        #region Public Constants and Readonly Fields

        /// <summary>Value pattern</summary>
        public static readonly AutomationPattern Pattern = AutomationPattern.Register(AutomationIdentifierConstants.Patterns.RangeValue, "RangeValuePatternIdentifiers.Pattern");

        /// <summary>Property ID: Value - Value of a value control, as a double</summary>
        public static readonly AutomationProperty ValueProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.RangeValueValue, "RangeValuePatternIdentifiers.ValueProperty");

        /// <summary>Property ID: IsReadOnly - Indicates that the value can only be read, not modified.</summary>
        public static readonly AutomationProperty IsReadOnlyProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.RangeValueIsReadOnly, "RangeValuePatternIdentifiers.IsReadOnlyProperty");

        /// <summary>Property ID: Maximum value</summary>
        public static readonly AutomationProperty MinimumProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.RangeValueMinimum, "RangeValuePatternIdentifiers.MinimumProperty");

        /// <summary>Property ID: Maximum value</summary>
        public static readonly AutomationProperty MaximumProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.RangeValueMaximum, "RangeValuePatternIdentifiers.MaximumProperty");

        /// <summary>Property ID: LargeChange - Indicates a value to be added to or subtracted from the Value property when the element is moved a large distance.</summary>
        public static readonly AutomationProperty LargeChangeProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.RangeValueLargeChange, "RangeValuePatternIdentifiers.LargeChangeProperty");

        /// <summary>Property ID: SmallChange - Indicates a value to be added to or subtracted from the Value property when the element is moved a small distance.</summary>
        public static readonly AutomationProperty SmallChangeProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.RangeValueSmallChange, "RangeValuePatternIdentifiers.SmallChangeProperty");

        #endregion Public Constants and Readonly Fields
    }
}
