// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Automation Identifiers for TableItem Pattern


using System;
using MS.Internal.Automation;

namespace System.Windows.Automation
{
    /// <summary>
    /// Used to expose grid items with header information.
    /// </summary>
#if (INTERNAL_COMPILE)
    internal static class TableItemPatternIdentifiers
#else
    public static class TableItemPatternIdentifiers
#endif
    {
        //------------------------------------------------------
        //
        //  Public Constants / Readonly Fields
        //
        //------------------------------------------------------
 
        #region Public Constants and Readonly Fields

        /// <summary>TableItem pattern</summary>
        public static readonly AutomationPattern Pattern = AutomationPattern.Register(AutomationIdentifierConstants.Patterns.TableItem, "TableItemPatternIdentifiers.Pattern");

        /// <summary>Property ID: RowHeaderItems - Collection of all row headers for this cell</summary>
        public static readonly AutomationProperty RowHeaderItemsProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.TableItemRowHeaderItems, "TableItemPatternIdentifiers.RowHeaderItemsProperty");

        /// <summary>Property ID: ColumnHeaderItems - Collection of all column headers for this cell</summary>
        public static readonly AutomationProperty ColumnHeaderItemsProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.TableItemColumnHeaderItems, "TableItemPatternIdentifiers.ColumnHeaderItemsProperty");

        #endregion Public Constants and Readonly Fields
    }
}
