// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;

using System;
using System.Security; 
using MS.Internal.PresentationCore; // for FriendAccessAllowed

namespace System.Windows.Input
{
    /// <summary>
    ///     The InputEventArgs class represents a type of RoutedEventArgs that
    ///     are relevant to all input events.
    /// </summary>

    [FriendAccessAllowed ] // expose UserInitiated 
    public class InputEventArgs : RoutedEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the InputEventArgs class.
        /// </summary>
        /// <param name="inputDevice">
        ///     The input device to associate with this event.
        /// </param>
        /// <param name="timestamp">
        ///     The time when the input occurred. 
        /// </param>
        public InputEventArgs(InputDevice inputDevice, int timestamp)
        {
            /* inputDevice parameter being null is valid*/
	    /* timestamp parameter is valuetype, need not be checked */
            _inputDevice = inputDevice;
            _timestamp = timestamp;
        }

        /// <summary>
        ///     Read-only access to the input device that initiated this
        ///     event.
        /// </summary>
        public InputDevice Device
        {
            get {return _inputDevice;}
            internal set {_inputDevice = value;}
        }

        /// <summary>
        ///     Read-only access to the input timestamp.
        /// </summary>
        public int Timestamp
        {
            get {return _timestamp;}
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
            InputEventHandler handler = (InputEventHandler) genericHandler;
            handler(genericTarget, this);
        }

        private InputDevice _inputDevice;
        private static int _timestamp;
     }
}

