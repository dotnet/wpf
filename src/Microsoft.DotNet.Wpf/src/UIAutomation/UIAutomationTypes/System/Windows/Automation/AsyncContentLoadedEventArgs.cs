// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: AsyncContentLoadedEventArgs event args class
//
//  
//
//
//

using System;
using System.Windows.Automation;

namespace System.Windows.Automation 
{
    /// <summary>
    /// AsyncContentLoadedEventArgs event args class
    /// </summary>
#if (INTERNAL_COMPILE)
    internal sealed class AsyncContentLoadedEventArgs  : AutomationEventArgs
#else
    public sealed class AsyncContentLoadedEventArgs  : AutomationEventArgs
#endif
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        /// <summary>
        /// Constructor for async content loaded event args.
        /// </summary>
        /// <param name="asyncContentState">Flag indicating the state of the content load.</param>
        /// <param name="percentComplete">Indicates percent complete for the content load.</param>
        public AsyncContentLoadedEventArgs (AsyncContentLoadedState asyncContentState, double percentComplete) 
            : base(AutomationElementIdentifiers.AsyncContentLoadedEvent) 
        {
            _asyncContentState = asyncContentState;
            _percentComplete = percentComplete;
        }

        #endregion Constructors


        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------
 
        #region Public Properties

        /// <summary>
        /// Returns the state of the content load.
        /// </summary>
        public AsyncContentLoadedState AsyncContentLoadedState 
        { 
            get 
            { 
                return _asyncContentState; 
            } 
        }

        /// <summary>
        /// Returns percent complete for the content load.
        /// </summary>
        public double PercentComplete 
        { 
            get 
            {
                return _percentComplete;
            } 
        }

        #endregion Public Properties


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields

        private AsyncContentLoadedState _asyncContentState;
        private double _percentComplete;

        #endregion Private Fields
    }
}
