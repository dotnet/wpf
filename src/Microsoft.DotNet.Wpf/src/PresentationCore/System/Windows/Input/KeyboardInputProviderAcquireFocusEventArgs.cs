// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.Windows.Input 
{
    /// <summary>
    ///     The KeyboardInputProviderAcquireFocusEventArgs class is used to
    ///     notify elements before and after keyboard focus is acquired through
    ///     a keyboard input provider.
    /// </summary>
    public class KeyboardInputProviderAcquireFocusEventArgs : KeyboardEventArgs
    {
        /// <summary>
        ///     Constructs an instance of the KeyboardInputProviderAcquireFocusEventArgs class.
        /// </summary>
        /// <param name="keyboard">
        ///     The logical keyboard device associated with this event.
        /// </param>
        /// <param name="timestamp">
        ///     The time when the input occured.
        /// </param>
        /// <param name="focusAcquired">
        ///     Whether or not interop focus was acquired.
        /// </param>
        public KeyboardInputProviderAcquireFocusEventArgs(KeyboardDevice keyboard, int timestamp, bool focusAcquired) : base(keyboard, timestamp)
        {
            _focusAcquired = focusAcquired;
        }

        /// <summary>
        ///     The element that now has focus.
        /// </summary>
        public bool FocusAcquired
        {
            get {return _focusAcquired;}
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
            KeyboardInputProviderAcquireFocusEventHandler handler = (KeyboardInputProviderAcquireFocusEventHandler) genericHandler;
            
            handler(genericTarget, this);
        }

        private bool _focusAcquired;
    }
}
