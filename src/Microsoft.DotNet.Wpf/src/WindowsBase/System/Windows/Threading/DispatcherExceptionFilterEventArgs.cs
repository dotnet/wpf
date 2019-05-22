// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: Arguments for the ExceptionFilter event. The event is raised 
//              when a dispatcher exception has occured. This event is raised
//              before the callstack is unwound.
//
//
//
//

using System.Diagnostics;

using System;

namespace System.Windows.Threading 
{
    /// <summary>
    ///     Arguments for the ExceptionFilter event. The event is raised when
    ///     a dispatcher exception has occured.
    /// </summary>
    /// <remarks>
    ///     This event is raised before the callstack is unwound.
    /// </remarks>
    public sealed class DispatcherUnhandledExceptionFilterEventArgs : DispatcherEventArgs
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        // Initialize a new event argument.
        internal DispatcherUnhandledExceptionFilterEventArgs(Dispatcher dispatcher)
            : base(dispatcher)
        {
        }

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        /// <summary>
        ///     The exception that was raised on a thread operating within
        ///     the dispatcher.
        /// </summary>
        public Exception Exception
        {
            get
            {
                return _exception;
            }
        }
        
        /// <summary>
        ///     Whether or not the exception should be caught and the exception
        ///     event handlers called.
        /// </summary>
        /// <remarks>
        ///     A filter handler can set this property to false to request that
        ///     the exception not be caught, to avoid the callstack getting
        ///     unwound up to the Dispatcher.
        ///     <P/>
        ///     A previous handler in the event multicast might have already set this 
        ///     property to false, signalling that the exception will not be caught.
        ///     We let the "don't catch" behavior override all others because
        ///     it most likely means a debugging scenario.
        /// </remarks>
        public bool RequestCatch
        {
            get
            {
                return _requestCatch;
            }
            set
            {
                // Only allow to be set false.
                if (value == false)
                {
                    _requestCatch = value;
                }
            }
        }

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        /// <summary>
        ///     Initialize the preallocated args class for use.
        /// </summary>
        /// <remarks>
        ///     This method MUST NOT FAIL because it is called from an exception
        ///     handler: do not do any heavy lifting or allocate more memory.
        ///     This initialization step is separated from the constructor
        ///     precisely because we wanted to preallocate the memory and avoid
        ///     hitting a secondary exception in the out-of-memory case.
        /// </remarks>
        /// <param name="exception">
        ///     The exception that was raised while executing code via the
        ///     dispatcher.
        /// </param>
        /// <param name="requestCatch">
        ///     Whether or not the exception should be caught and the
        ///     exception handlers called.
        /// </param>
        internal void Initialize(Exception exception, bool requestCatch)
        {
            Debug.Assert(exception != null);
            _exception = exception;
            _requestCatch = requestCatch;
        }

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        private Exception   _exception;
        private bool        _requestCatch;
    }

    /// <summary>
    ///   Delegate for the events that convey the state of the UiConext
    ///   in response to various actions that involve items.
    /// </summary>
    public delegate void DispatcherUnhandledExceptionFilterEventHandler(object sender, DispatcherUnhandledExceptionFilterEventArgs e);
}

