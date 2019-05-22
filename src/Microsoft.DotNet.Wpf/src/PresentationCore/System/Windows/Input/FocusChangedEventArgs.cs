// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Input 
{
    /// <summary>
    ///     The KeyboardFocusChangedEventArgs class contains information about key states.
    /// </summary>
    public class KeyboardFocusChangedEventArgs : KeyboardEventArgs
    {
        /// <summary>
        ///     Constructs an instance of the KeyboardFocusChangedEventArgs class.
        /// </summary>
        /// <param name="keyboard">
        ///     The logical keyboard device associated with this event.
        /// </param>
        /// <param name="timestamp">
        ///     The time when the input occured.
        /// </param>
        /// <param name="oldFocus">
        ///     The element that previously had focus.
        /// </param>
        /// <param name="newFocus">
        ///     The element that now has focus.
        /// </param>
        public KeyboardFocusChangedEventArgs(KeyboardDevice keyboard, int timestamp, IInputElement oldFocus, IInputElement newFocus) : base(keyboard, timestamp)
        {
            if (oldFocus != null && !InputElement.IsValid(oldFocus))
                throw new InvalidOperationException(SR.Get(SRID.Invalid_IInputElement, oldFocus.GetType()));

            if (newFocus != null && !InputElement.IsValid(newFocus))
                throw new InvalidOperationException(SR.Get(SRID.Invalid_IInputElement, newFocus.GetType()));

            _oldFocus = oldFocus;
            _newFocus = newFocus;
        }

        /// <summary>
        ///     The element that previously had focus.
        /// </summary>
        public IInputElement OldFocus
        {
            get {return _oldFocus;}
        }

        /// <summary>
        ///     The element that now has focus.
        /// </summary>
        public IInputElement NewFocus
        {
            get {return _newFocus;}
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
            KeyboardFocusChangedEventHandler handler = (KeyboardFocusChangedEventHandler) genericHandler;
            
            handler(genericTarget, this);
        }

        private IInputElement _oldFocus;
        private IInputElement _newFocus;
    }
}


