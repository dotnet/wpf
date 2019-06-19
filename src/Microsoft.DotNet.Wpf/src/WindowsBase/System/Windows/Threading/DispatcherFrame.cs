// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security; 

namespace System.Windows.Threading
{
    /// <summary>
    ///     Representation of Dispatcher frame.
    /// </summary>
    public class DispatcherFrame : DispatcherObject
    {
        static DispatcherFrame()
        {
        }

        /// <summary>
        ///     Constructs a new instance of the DispatcherFrame class.
        /// </summary>
        public DispatcherFrame() : this(true)
        {
        }
        
        /// <summary>
        ///     Constructs a new instance of the DispatcherFrame class.
        /// </summary>
        /// <param name="exitWhenRequested">
        ///     Indicates whether or not this frame will exit when all frames
        ///     are requested to exit.
        ///     <p/>
        ///     Dispatcher frames typically break down into two categories:
        ///     1) Long running, general purpose frames, that exit only when
        ///        told to.  These frames should exit when requested.
        ///     2) Short running, very specific frames that exit themselves
        ///        when an important criteria is met.  These frames may
        ///        consider not exiting when requested in favor of waiting
        ///        for their important criteria to be met.  These frames
        ///        should have a timeout associated with them.
        /// </param>
        public DispatcherFrame(bool exitWhenRequested)
        {
            _exitWhenRequested = exitWhenRequested;
            _continue = true;
        }

        /// <summary>
        ///     Indicates that this dispatcher frame should exit.
        /// </summary>
        public bool Continue
        {
            get
            {
                // This method is free-threaded.
                    
                // First check if this frame wants to continue.
                bool shouldContinue = _continue;
                if(shouldContinue)
                {
                    // This frame wants to continue, so next check if it will
                    // respect the "exit requests" from the dispatcher.
                    if(_exitWhenRequested)
                    {
                        Dispatcher dispatcher = Dispatcher;
                        
                        // This frame is willing to respect the "exit requests" of
                        // the dispatcher, so check them.
                        if(dispatcher._exitAllFrames || dispatcher._hasShutdownStarted)
                        {
                            shouldContinue = false;
                        }
                    }
                }
                
                return shouldContinue;
            }

            set
            {
                // This method is free-threaded.

                _continue = value;

                // Post a message so that the message pump will wake up and
                // check our continue state.
                Dispatcher.BeginInvoke(DispatcherPriority.Send, (DispatcherOperationCallback) delegate(object unused) {return null;}, null);
            }
        }

        private bool _exitWhenRequested;
        private bool _continue;
    }
}
