// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Automation Identifiers for GridItem Pattern

using System;
using MS.Internal.Automation;

namespace System.Windows.Automation
{
    /// <summary>
    /// Allows clients to quickly determine if an item they discover is part
    /// of a grid and, if so, where the item is in the grid in terms of row/column coordinates
    /// and spans.
    /// </summary>
#if (INTERNAL_COMPILE)
    internal static class GridItemPatternIdentifiers
#else
    public static class GridItemPatternIdentifiers
#endif
    {
        //------------------------------------------------------
        //
        //  Public Constants / Readonly Fields
        //
        //------------------------------------------------------
 
        #region Public Constants and Readonly Fields

        /// <summary>GridItem pattern</summary>
        public static readonly AutomationPattern Pattern = AutomationPattern.Register(AutomationIdentifierConstants.Patterns.GridItem, "GridItemPatternIdentifiers.Pattern");

        /// <summary>RowCount</summary>
        public static readonly AutomationProperty RowProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.GridItemRow, "GridItemPatternIdentifiers.RowProperty");

        /// <summary>ColumnCount</summary>
        public static readonly AutomationProperty ColumnProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.GridItemColumn, "GridItemPatternIdentifiers.ColumnProperty");

        /// <summary>RowSpan</summary>
        public static readonly AutomationProperty RowSpanProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.GridItemRowSpan, "GridItemPatternIdentifiers.RowSpanProperty");

        /// <summary>ColumnSpan</summary>
        public static readonly AutomationProperty ColumnSpanProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.GridItemColumnSpan, "GridItemPatternIdentifiers.ColumnSpanProperty");

        /// <summary>The logical element that supports the GripPattern for this Item</summary>
        public static readonly AutomationProperty ContainingGridProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.GridItemContainingGrid, "GridItemPatternIdentifiers.ContainingGridProperty");

        #endregion Public Constants and Readonly Fields
    }
}
