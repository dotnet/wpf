// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
// Description:
//          This event is fired when the application starts  - once that application’s Run() 
//          method has been called. 
//
//          The developer will typically hook this event if they want to take action at startup time 
//

using System.ComponentModel;

using System.Windows.Interop;
using MS.Internal.PresentationFramework;
using System.Runtime.CompilerServices;
using MS.Internal;
using MS.Internal.AppModel; 

namespace System.Windows
{
    /// <summary>
    /// Event args for Startup event
    /// </summary>
    public class StartupEventArgs : EventArgs
    {
        /// <summary>
        /// constructor
        /// </summary>
        internal StartupEventArgs()
        {
            _performDefaultAction = true;
        }


        /// <summary>
        /// Command Line arguments
        /// </summary>
        public String[] Args
        {
            get
            {
                if (_args == null)
                {
                    _args = GetCmdLineArgs();
                }
                return _args;
            }
        }

        internal bool PerformDefaultAction
        {
            get { return _performDefaultAction; }
            set { _performDefaultAction = value; }
        }


        private string[] GetCmdLineArgs()
        {
            string[] args = Environment.GetCommandLineArgs();
            Invariant.Assert(args.Length >= 1);

            int newLength = args.Length - 1;
            newLength = (newLength >= 0 ? newLength : 0);

            string[] retValue = new string[newLength];

            for (int i = 1; i < args.Length; i++)
            {
                retValue[i - 1] = args[i];
            }

            return retValue;
        }
        

        private String[]    _args;
        private bool        _performDefaultAction;
    }
}
