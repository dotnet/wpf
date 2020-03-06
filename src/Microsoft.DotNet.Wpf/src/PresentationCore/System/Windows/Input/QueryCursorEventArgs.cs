// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.Windows.Input
{
    /// <summary>
    ///     Provides data for the QueryCursor event.
    /// </summary>
    public class QueryCursorEventArgs : MouseEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the QueryCursorEventArgs class.
        /// </summary>
        /// <param name="mouse">
        ///     The logical Mouse device associated with this event.
        /// </param>
        /// <param name="timestamp">
        ///     The time when the input occurred.
        /// </param>
        public QueryCursorEventArgs(MouseDevice mouse, int timestamp) : base(mouse, timestamp)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the QueryCursorEventArgs class.
        /// </summary>
        /// <param name="mouse">
        ///     The logical Mouse device associated with this event.
        /// </param>
        /// <param name="timestamp">
        ///     The time when the input occurred.
        /// </param>
        /// <param name="stylusDevice">
        ///     The stylus pointer that was involved with this event.
        /// </param>
        public QueryCursorEventArgs(MouseDevice mouse, int timestamp, StylusDevice stylusDevice) : base(mouse, timestamp, stylusDevice)
        {
        }

        /// <summary>
        ///     The cursor to set.
        /// </summary>
        public Cursor Cursor
        {
            get {return _cursor;}
            set {_cursor = ((value == null) ? Cursors.None : value);}
        }

        /// <summary>
        ///     The mechanism used to call the type-specific handler on the
        ///     target.
        /// </summary>
        /// <param name="genericHandler">
        ///     The generic handler to call in a type-specific way.
        /// </param>
        /// <param name="genericTarget">
        ///     The target to call the handler on.
        /// </param>
        protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
        {
            QueryCursorEventHandler handler = (QueryCursorEventHandler) genericHandler;
            handler(genericTarget, this);
        }

        private Cursor _cursor;
    }
}

