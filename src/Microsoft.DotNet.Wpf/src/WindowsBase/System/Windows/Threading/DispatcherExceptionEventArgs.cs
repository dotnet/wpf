// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: Arguments for the events that convey an exception that was
//              raised while executing code via the Dispatcher.
//
//
//
//
//

using System.Diagnostics;
using System;

namespace System.Windows.Threading
{
    /// <summary>
    ///   Arguments for the events that convey an exception that was raised
    ///   while executing code via the dispatcher.
    /// </summary>
    public sealed class DispatcherUnhandledExceptionEventArgs : DispatcherEventArgs
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        // Initialize a new event argument.
        internal DispatcherUnhandledExceptionEventArgs(Dispatcher dispatcher)
            : base(dispatcher)
        {
        }

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        /// <summary>
        ///     The exception that was raised while executing code via the
        ///     dispatcher.
        /// </summary>
        public Exception Exception
        {
            get
            {
                return _exception;
            }
        }

        /// <summary>
        ///     Whether or not the exception event has been handled.
        ///     Other handlers should respect this field, and not display any
        ///     UI in response to being notified.  Passive responses (such as
        ///     logging) can still be done.
        ///     If no handler sets this value to true, default UI will be shown.
        /// </summary>
        public bool Handled
        {
            get
            {
                return _handled;
            }
            set
            {
                // Only allow to be set true.
                if (value == true)
                {
                    _handled = value;
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
        ///     dispatcher
        /// </param>
        /// <param name="handled">
        ///     Whether or not the exception has been handled
        /// </param>
        internal void Initialize(Exception exception, bool handled)
        {
            Debug.Assert(exception != null);
            _exception = exception;
            _handled = handled;
        }

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        private Exception _exception;
        private bool _handled;
    }

    /// <summary>
    ///   Delegate for the events that convey the state of the UiConext
    ///   in response to various actions that involve items.
    /// </summary>
    public delegate void DispatcherUnhandledExceptionEventHandler(object sender, DispatcherUnhandledExceptionEventArgs e);
}

