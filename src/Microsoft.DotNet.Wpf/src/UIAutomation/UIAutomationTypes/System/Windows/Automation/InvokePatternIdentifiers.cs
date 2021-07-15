// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Automation Identifiers for Invoke Pattern

using System;
using MS.Internal.Automation;

namespace System.Windows.Automation
{


    /// <summary>
    /// Represents objects that have a single, unambiguous, action associated with them.
    /// 
    /// Examples of UI that implments this includes:
    /// Push buttons
    /// Hyperlinks
    /// Menu items
    /// Radio buttons
    /// Check boxes
    /// </summary>
#if (INTERNAL_COMPILE)
    internal static class InvokePatternIdentifiers
#else
    public static class InvokePatternIdentifiers
#endif
    {
        //------------------------------------------------------
        //
        //  Public Constants / Readonly Fields
        //
        //------------------------------------------------------
 
        #region Public Constants and Readonly Fields

        /// <summary>Invokable pattern</summary>
        public static readonly AutomationPattern Pattern = AutomationPattern.Register(AutomationIdentifierConstants.Patterns.Invoke, "InvokePatternIdentifiers.Pattern");

        /// <summary>Event ID: Invoked - event used to watch for Invokable pattern Invoked events</summary>
        public static readonly AutomationEvent InvokedEvent = AutomationEvent.Register(AutomationIdentifierConstants.Events.Invoke_Invoked, "InvokePatternIdentifiers.InvokedEvent");

        #endregion Public Constants and Readonly Fields
    }
}
