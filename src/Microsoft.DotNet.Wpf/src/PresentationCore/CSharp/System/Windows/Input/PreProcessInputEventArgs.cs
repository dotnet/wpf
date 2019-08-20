// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Security; 

namespace System.Windows.Input 
{
    /// <summary>
    ///     Allows the handler to cancel the processing of an input event. 
    /// </summary>
    /// <remarks>
    ///     An instance of this class is passed to the handlers of the
    ///     following events:
    ///     <list>
    ///         <item>
    ///             <see cref="InputManager.PreProcessInput"/>
    ///         </item>
    ///     </list>
    /// </remarks>
    public sealed class PreProcessInputEventArgs : ProcessInputEventArgs
    {
        // Only we can make these.  Note that we cache and reuse instances.
        internal PreProcessInputEventArgs() {}

        internal override void Reset(StagingAreaInputItem input, InputManager inputManager)
        {
            _canceled = false;
            base.Reset(input, inputManager);
        }
        
        /// <summary>
        ///     Cancels the processing of the input event.
        /// </summary>
        public void Cancel()
        {
            _canceled = true;
        }

        /// <summary>
        ///     Whether or not the input event processing was canceled.
        /// </summary>
        public bool Canceled {get {return _canceled;}}

        private bool _canceled;
    }

    /// <summary>
    ///     Delegate type for handles of events that use
    ///     <see cref="PreProcessInputEventArgs"/>.
    /// </summary>
    public delegate void PreProcessInputEventHandler(object sender, PreProcessInputEventArgs e);
}

