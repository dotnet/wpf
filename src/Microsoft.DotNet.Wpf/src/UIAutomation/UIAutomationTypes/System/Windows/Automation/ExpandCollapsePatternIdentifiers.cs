// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Automation Identifiers for ExpandCollapse Pattern

using System;
using MS.Internal.Automation;
using System.Runtime.InteropServices;

namespace System.Windows.Automation
{

    /// <summary>
    /// Used by ExpandCollapse pattern to indicate expanded/collapsed state
    /// </summary>
    [ComVisible(true)]
    [Guid("76d12d7e-b227-4417-9ce2-42642ffa896a")]
#if (NO_INTERNAL_COMPILE_BUG1080665)
    internal enum ExpandCollapseState
#else
    public enum ExpandCollapseState
#endif
    {
        /// <summary>No children are showing</summary>
        Collapsed,
        /// <summary>All children are showing</summary>
        Expanded,
        /// <summary>Not all children are showing</summary>
        PartiallyExpanded,
        /// <summary>Does not expand or collapse</summary>
        LeafNode
    }

       
    ///<summary>wrapper class for ExpandCollapse pattern </summary>
#if (INTERNAL_COMPILE)
    internal static class ExpandCollapsePatternIdentifiers
#else
    public static class ExpandCollapsePatternIdentifiers
#endif
    {
        //------------------------------------------------------
        //
        //  Public Constants / Readonly Fields
        //
        //------------------------------------------------------
 
        #region Public Constants and Readonly Fields

        /// <summary>Scroll pattern</summary>
        public static readonly AutomationPattern Pattern = AutomationPattern.Register(AutomationIdentifierConstants.Patterns.ExpandCollapse, "ExpandCollapsePatternIdentifiers.Pattern");

        /// <summary>Property ID: ExpandCollapseState - Current Collapsed or Expanded state</summary>
        public static readonly AutomationProperty ExpandCollapseStateProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.ExpandCollapseExpandCollapseState, "ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty");

        #endregion Public Constants and Readonly Fields
    }
}
