// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace System.Windows.Threading
{
    /// <summary>
    ///     Additional information provided about a dispatcher.
    /// </summary>
    public sealed class DispatcherHookEventArgs : EventArgs
    {
        /// <summary>
        ///     Constructs an instance of the DispatcherHookEventArgs class.
        /// </summary>
        /// <param name="operation">
        ///     The operation in question.
        /// </param>
        public DispatcherHookEventArgs(DispatcherOperation operation)
        {
            _operation = operation;
        }

        /// <summary>
        ///     The dispatcher effected.
        /// </summary>
        public Dispatcher Dispatcher
        {
            get
            {
                return _operation != null ? _operation.Dispatcher : null;
            }
        }
        
        /// <summary>
        ///     The operation effected.
        /// </summary>
        public DispatcherOperation Operation
        {
            get
            {
                return _operation;
            }
        }

        private DispatcherOperation _operation;
    }

    /// <summary>
    ///     The handler for the DispatcherHookEventArgs.
    /// </summary>
    public delegate void DispatcherHookEventHandler(object sender, DispatcherHookEventArgs e);
}

