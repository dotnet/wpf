// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Automation Identifiers for Table Pattern

using System;
using MS.Internal.Automation;
using System.Runtime.InteropServices;

namespace System.Windows.Automation
{
    /// <summary>
    ///  Is the data data in this table best present by row or column
    /// </summary>
    [ComVisible(true)]
    [Guid("15fdf2e2-9847-41cd-95dd-510612a025ea")]
#if (NO_INTERNAL_COMPILE_BUG1080665)
    internal enum RowOrColumnMajor
#else
    public enum RowOrColumnMajor
#endif
    {
        /// <summary>Data in the table should be read row by row</summary>
        RowMajor,
        /// <summary>Data in the table should be read column by column</summary>
        ColumnMajor,
        /// <summary>There is no way to determine the best way to present the data</summary>
        Indeterminate,
    }
    
    /// <summary>
    /// Identifies a grid that has header information.
    /// </summary>
#if (INTERNAL_COMPILE)
    internal static class TablePatternIdentifiers
#else
    public static class TablePatternIdentifiers
#endif
    {
        //------------------------------------------------------
        //
        //  Public Constants / Readonly Fields
        //
        //------------------------------------------------------
 
        #region Public Constants and Readonly Fields

        /// <summary>Table pattern</summary>
        public static readonly AutomationPattern Pattern = AutomationPattern.Register(AutomationIdentifierConstants.Patterns.Table, "TablePatternIdentifiers.Pattern");

        /// <summary>Property ID: RowHeaders - Collection of all row headers for this table</summary>
        public static readonly AutomationProperty RowHeadersProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.TableRowHeaders, "TablePatternIdentifiers.RowHeadersProperty");

        /// <summary>Property ID: ColumnHeaders - Collection of all column headers for this table</summary>
        public static readonly AutomationProperty ColumnHeadersProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.TableColumnHeaders, "TablePatternIdentifiers.ColumnHeadersProperty");

        /// <summary>Property ID: RowOrColumnMajor - Indicates if the data is best presented by row or column</summary>
        public static readonly AutomationProperty RowOrColumnMajorProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.TableRowOrColumnMajor, "TablePatternIdentifiers.RowOrColumnMajorProperty");

        #endregion Public Constants and Readonly Fields
    }
}
