// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Focus event args class (Client Side)

using System;
using System.Windows.Automation.Provider;
using MS.Internal.Automation;

namespace System.Windows.Automation 
{
    /// <summary>
    /// Delegate to handle focus change events
    /// </summary>
#if (INTERNAL_COMPILE)
    internal delegate void AutomationFocusChangedEventHandler( object sender, AutomationFocusChangedEventArgs e );
#else
    public delegate void AutomationFocusChangedEventHandler( object sender, AutomationFocusChangedEventArgs e );
#endif

    // AutomationFocusChangedEventArgs has two distinct uses:
    // - when used by provider code, it is basically a wrapper for the previous focus provider,
    //   which gets passed through as a parameter to the UiaCore API.
    // - on the client side, it is used to deliver focus events to the client - these events
    //   may originate within the client via focus winevents, or may be the result of notifications
    //   from core. In both cases, the ClientApi code creates an instance of the args, with an
    //   AutomationElement indicating the previous focus.

    /// <summary>
    /// Focus event args class
    /// </summary>
#if (INTERNAL_COMPILE)
    internal class AutomationFocusChangedEventArgs : AutomationEventArgs
#else
    public class AutomationFocusChangedEventArgs : AutomationEventArgs
#endif
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        /// <summary>
        /// Client-side constructor for focus event args.
        /// </summary>
        /// <param name="idObject">Indicates object id.</param>
        /// <param name="idChild">Indicates id of the child.</param>
        public AutomationFocusChangedEventArgs(int idObject, int idChild)
            : base(AutomationElement.AutomationFocusChangedEvent) 
        {
            _idObject = idObject;
            _idChild = idChild;
        }
        #endregion Constructors


        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------
 
        #region Public Properties

        /// <summary>
        /// Returns the object id
        /// </summary>
        public int ObjectId
        { 
            get
            {
                return _idObject;
            } 
        }

        /// <summary>
        /// Returns the child id
        /// </summary>
        public int ChildId
        { 
            get 
            {
                return _idChild; 
            } 
        }

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields

        private int _idObject;    // needed for now to be able to go back to IAccessible for menus/dropdowns
        private int _idChild;     // needed for now to be able to go back to IAccessible for menus/dropdowns

        #endregion Private Fields
    }
}
