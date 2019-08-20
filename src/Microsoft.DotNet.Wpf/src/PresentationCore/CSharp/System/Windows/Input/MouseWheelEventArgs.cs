// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;

namespace System.Windows.Input
{
    /// <summary>
    ///     The MouseWheelEventArgs describes the state of a Mouse wheel.
    /// </summary>
    public class MouseWheelEventArgs : MouseEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the MouseWheelEventArgs class.
        /// </summary>
        /// <param name="mouse">
        ///     The Mouse device associated with this event.
        /// </param>
        /// <param name="timestamp">
        ///     The time when the input occured.
        /// </param>
        /// <param name="delta">
        ///     How much the mouse wheel turned.
        /// </param>
        public MouseWheelEventArgs(MouseDevice mouse, int timestamp, int delta) : base(mouse, timestamp)
        {
            _delta = delta;
        }

        /// <summary>
        ///     Read-only access to the amount the mouse wheel turned.
        /// </summary>
        public int Delta
        {
            get {return _delta;}
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
            MouseWheelEventHandler handler = (MouseWheelEventHandler) genericHandler;
            handler(genericTarget, this);
        }

        private static int _delta;
    }
}
