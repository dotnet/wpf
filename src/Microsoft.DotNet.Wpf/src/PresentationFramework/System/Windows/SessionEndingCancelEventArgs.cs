// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
// Description:
//              OnSessionEnding is called to raise the SessionEnding event. The developer will 
//              typically override this method if they want to take action when the OS is ending 
//              a session ( or they may choose to attach an event). 
//
//              This method will be called when the user has chosen to either logoff or shutdown. 
//              These events are equivalent to receiving a WM_QUERYSESSION window event. 
//              Windows will send it when user is logging out/shutting down. 
//              ( See http://msdn.microsoft.com/library/default.asp?url=/library/en-us/sysinfo/base/wm_queryendsession.asp ). 
//
//              By default if this event is not cancelled – Avalon will then call Application.Shutdown.
//

using System.ComponentModel;

namespace System.Windows
{
    /// <summary>
    /// Event args for StartingUp event
    /// </summary>
    public class SessionEndingCancelEventArgs : CancelEventArgs
    {
        /// <summary>
        /// constructor
        /// </summary>
        internal SessionEndingCancelEventArgs(ReasonSessionEnding reasonSessionEnding)
        {
            _reasonSessionEnding = reasonSessionEnding;
        }

        /// <summary>
        ///     The ReasonSessionEnding enum on the  SessionEndingEventArgs indicates whether 
        ///     the session is ending in response to a shutdown of the OS, or if the user 
        ///     is logging off
        /// </summary>
        public ReasonSessionEnding ReasonSessionEnding
        {
            get
            {
                return _reasonSessionEnding;
            }
        }

        private ReasonSessionEnding _reasonSessionEnding;
    }
}
