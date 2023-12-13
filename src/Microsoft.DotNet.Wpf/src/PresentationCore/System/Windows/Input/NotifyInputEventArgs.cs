// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Security ;
using MS.Internal; 
using MS.Internal.PresentationCore;                        // SecurityHelper

namespace System.Windows.Input 
{
    /// <summary>
    ///     Provides information about an input event being processed by the
    ///     input manager.
    /// </summary>
    /// <remarks>
    ///     An instance of this class, or a derived class, is passed to the
    ///     handlers of the following events:
    ///     <list>
    ///     </list>
    /// </remarks>
    public class NotifyInputEventArgs : EventArgs
    {
        // Only we can make these.  Note that we cache and reuse instances.
        internal NotifyInputEventArgs() {}
        
        internal virtual void Reset(StagingAreaInputItem input, InputManager inputManager)
        {
            _input = input;
            _inputManager = inputManager;
        }

        /// <summary>
        ///     The staging area input item being processed by the input
        ///     manager.
        /// </summary>
        public StagingAreaInputItem StagingItem {get {return _input;}}

        /// <summary>
        ///     The input manager processing the input event.
        /// </summary>
        public InputManager InputManager 
        {
            get 
            {
                return _inputManager;
            }
        }

        /// <summary>
        ///     The input manager processing the input event.
        ///     *** FOR INTERNAL USE ONLY **** 
        /// </summary>
        internal InputManager UnsecureInputManager 
        {
            get 
            {
                return _inputManager;
            }
        }
        
        private StagingAreaInputItem _input;

        private InputManager _inputManager;
}

    /// <summary>
    ///     Delegate type for handles of events that use
    ///     <see cref="NotifyInputEventArgs"/>.
    /// </summary>
    public delegate void NotifyInputEventHandler(object sender, NotifyInputEventArgs e);
}


