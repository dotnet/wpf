// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
// Description:
//          This event will be called after Shutdown has been called. 
//
//          The developer will hook this event if they want to take action 
//          when the application exits.
//


namespace System.Windows
{
    /// <summary>
    /// Event args for the Exit event
    /// </summary>
    public class ExitEventArgs : EventArgs
    {
        internal int _exitCode;

        /// <summary>
        /// constructor
        /// </summary>
        internal ExitEventArgs(int exitCode)
        {
            _exitCode = exitCode;
        }

        /// <summary>
        /// Get and set the exit code to be returned by this application
        /// </summary>
        public int ApplicationExitCode
        {
            get { return _exitCode; }
            set { _exitCode = value; }
        }
    }
}
