// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Automation Identifiers for Dock Pattern

using System;
using MS.Internal.Automation;
using System.Runtime.InteropServices;

namespace System.Windows.Automation
{

    /// <summary>
    /// The edge of the container that the dockable window will cling to.
    /// </summary>
    [ComVisible(true)]
    [Guid("70d46e77-e3a8-449d-913c-e30eb2afecdb")]
#if (NO_INTERNAL_COMPILE_BUG1080665)
    internal enum DockPosition
#else
    public enum DockPosition
#endif
    {
        /// <summary>Docked at the top</summary>
        Top,
        /// <summary>Docked at the left</summary>
        Left,
        /// <summary>Docked at the bottom</summary>
        Bottom,
        /// <summary>Docked at the right</summary>
        Right,
        /// <summary>Docked on all four sides</summary>
        Fill,
        /// <summary>Not docked</summary>
        None
    }
    
    /// <summary>
    /// Expose an element's ability to change its dock state at run time.
    /// </summary>
#if (INTERNAL_COMPILE)
    internal static class DockPatternIdentifiers
#else
    public static class DockPatternIdentifiers
#endif
    {
        //------------------------------------------------------
        //
        //  Public Constants / Readonly Fields
        //
        //------------------------------------------------------
 
        #region Public Constants and Readonly Fields

        /// <summary>Dock pattern</summary>
        public static readonly AutomationPattern Pattern = AutomationPattern.Register(AutomationIdentifierConstants.Patterns.Dock, "DockPatternIdentifiers.Pattern");

        /// <summary>Property ID: DockPosition - Is the DockPosition Top, Left, Bottom, Right, Fill, or None</summary>
        public static readonly AutomationProperty DockPositionProperty = AutomationProperty.Register(AutomationIdentifierConstants.Properties.DockDockPosition, "DockPatternIdentifiers.DockPositionProperty");

        #endregion Public Constants and Readonly Fields
    }
}
