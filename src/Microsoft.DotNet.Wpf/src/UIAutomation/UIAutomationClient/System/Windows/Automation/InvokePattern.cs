// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Client-side wrapper for Invoke Pattern

using System;
using System.Windows.Automation.Provider;
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
    internal class InvokePattern: BasePattern
#else
    public class InvokePattern: BasePattern
#endif
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        private InvokePattern(AutomationElement el, SafePatternHandle hPattern)
            : base(el, hPattern)
        {
            _hPattern = hPattern;
        }

        #endregion Constructors


        //------------------------------------------------------
        //
        //  Public Constants / Readonly Fields
        //
        //------------------------------------------------------
 
        #region Public Constants and Readonly Fields

        /// <summary>Invokable pattern</summary>
        public static readonly AutomationPattern Pattern = InvokePatternIdentifiers.Pattern;

        /// <summary>Event ID: Invoked - event used to watch for Invokable pattern Invoked events</summary>
        public static readonly AutomationEvent InvokedEvent = InvokePatternIdentifiers.InvokedEvent;

        #endregion Public Constants and Readonly Fields


        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------
 
        #region Public Methods

        /// <summary>
        /// Request that the control initiate its action.
        /// Should return immediately without blocking.
        /// There is no way to determine what happened, when it happend, or whether
        /// anything happened at all.
        /// </summary>
        public void Invoke()
        {
            UiaCoreApi.InvokePattern_Invoke(_hPattern);
        }

        #endregion Public Methods


        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------
 
        #region Public Properties

        // No properties

        #endregion Public Properties


        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
 
        #region Internal Methods

        internal static object Wrap(AutomationElement el, SafePatternHandle hPattern, bool cached)
        {
            return new InvokePattern(el, hPattern);
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields

        private SafePatternHandle _hPattern;

        #endregion Private Fields
    }
}
