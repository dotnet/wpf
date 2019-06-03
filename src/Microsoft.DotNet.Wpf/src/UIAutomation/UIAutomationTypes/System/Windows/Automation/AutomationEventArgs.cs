// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Pattern or custom event args class

using System;
using System.Windows.Automation;

namespace System.Windows.Automation 
{
    /// <summary>
    /// Delegate to handle AutomationEvents
    /// </summary>
#if (INTERNAL_COMPILE)
    internal delegate void AutomationEventHandler( object sender, AutomationEventArgs e );
#else
    public delegate void AutomationEventHandler( object sender, AutomationEventArgs e );
#endif

    /// <summary>
    /// Pattern or custom event args class
    /// </summary>
#if (INTERNAL_COMPILE)
    internal class AutomationEventArgs: EventArgs
#else
    public class AutomationEventArgs: EventArgs
#endif
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        /// <summary>
        /// Constructor for pattern or custom event args.
        /// </summary>
        /// <param name="eventId">Indicates which event this instance represents.</param>
        public AutomationEventArgs(AutomationEvent eventId )
        {
            _eventId = eventId;
        }

        #endregion Constructors


        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------
 
        #region Public Properties

        /// <summary>
        /// EventId indicating which event this instance represents.
        /// </summary>
        public AutomationEvent EventId
        { 
            get
            {
                return _eventId;
            } 
        }
 
        #endregion Public Properties


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields

        private AutomationEvent _eventId;

        #endregion Private Fields
   }
}
