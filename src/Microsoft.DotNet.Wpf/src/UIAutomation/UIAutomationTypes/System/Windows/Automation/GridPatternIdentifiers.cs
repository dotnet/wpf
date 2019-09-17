// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Automation Identifiers for Grid Pattern

using System;
using MS.Internal.Automation;

namespace System.Windows.Automation
{
    ///<summary></summary>
#if (INTERNAL_COMPILE)
    internal static class GridPatternIdentifiers
#else
    public static class GridPatternIdentifiers
#endif
    {
        //------------------------------------------------------
        //
        //  Public Constants / Readonly Fields
        //
        //------------------------------------------------------
 
        #region Public Constants and Readonly Fields

        /// <summary>Grid pattern</summary>
        public static readonly AutomationPattern Pattern = AutomationPattern.Register(AutomationIdentifierConstants.Patterns.Grid, "GridPatternIdentifiers.Pattern");

        /// <summary>RowCount</summary>
        public static readonly AutomationProperty RowCountProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.GridRowCount, "GridPatternIdentifiers.RowCountProperty");

        /// <summary>ColumnCount</summary>
        public static readonly AutomationProperty ColumnCountProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.GridColumnCount, "GridPatternIdentifiers.ColumnCountProperty");

        #endregion Public Constants and Readonly Fields
    }
}
