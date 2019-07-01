// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.IO;
using System.Threading;
using Microsoft.Test.Logging;
using Microsoft.Test.Loaders;
using System.Windows.Automation;
using Microsoft.Test.Utilities;
using S = Microsoft.Test.Utilities.Scripter;

namespace Microsoft.Test.Loaders.UIHandlers 
{
    /// <summary>
    /// Scenarios UI handler
    /// </summary>
    public class CustomUIHandler: UIHandler
    {
        /// <summary>
        /// CustomUIHandler constructor
        /// </summary>
        public CustomUIHandler()
        {
        }

        /// <summary>
        /// Path of the script file
        /// </summary>
        public string ScriptFile = null;

        /// <summary>
        /// Handler (UIScript, Scripter, etc)
        /// </summary>
        public string Handler = null;

        /// <summary>
        /// HandlerID
        /// </summary>
        public struct HandlerID
        {
            /// <summary>
            /// scripter
            /// </summary>

            public const string Scripter = "Scripter";
            /// <summary>
            /// uiscript
            /// </summary>
            public const string UIScript = "UIScript";
        }

        /// <summary>
        /// Pass when done
        /// </summary>
        public string SetPassWhenDone = Boolean.TrueString;

        /// <summary>
        /// HandleWindow
        /// </summary>
        /// <param name="topHwnd"></param>
        /// <param name="hwnd"></param>
        /// <param name="process"></param>
        /// <param name="title"></param>
        /// <param name="notification"></param>
        /// <returns></returns>
        public override UIHandlerAction HandleWindow(System.IntPtr topHwnd, System.IntPtr hwnd, System.Diagnostics.Process process, string title, UIHandlerNotification notification)
        {
            ILog log = null;
            FileStream f = null;
            try
            {
                // create a log
                log = LogFactory.Create();
                log.StatusMessage = "CustomUIHandler invoked";

                // wait when invoked; allow UIA properties to get in sync with Win32
                Thread.Sleep(1000);

                // get a stream to the script file
                f = new FileStream(ScriptFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                // process the script file with the requested handler
                switch (Handler)
                {
                    case HandlerID.Scripter:
                        {
                            S.Scripter script = new S.Scripter(f, log);
                        }
                        break;
                    default:
                        {
                            UIScript script = new UIScript(f, topHwnd, log);
                            script.Execute();
                        }
                        break;
                }

                // we got here. it means script was executed with no exceptions
                if (String.Compare(SetPassWhenDone, Boolean.TrueString, true, CultureInfo.InvariantCulture) == 0)
                {
                    log.PassMessage = "Test Passed. Script executed with no exceptions and setting result was requested";
                }
                else if (String.Compare(SetPassWhenDone, Boolean.FalseString, true, CultureInfo.InvariantCulture) == 0)
                {
                    log.StatusMessage = "CustomUIHandler done - not setting result";
                }
                else
                {
                    log.FailMessage = "Invalid 'SetPassWhenDone' property value specified";
                }

                // end handler
                return (UIHandlerAction.Abort);
            }
            catch (Exception e)
            {
                if (log != null)
                {
                    log.FailMessage = "Exception caught in the UIHandler: " + e.ToString();
                }

                // end handler
                return (UIHandlerAction.Abort);
            }
            finally
            {
                // close the stream file if it was successfully created
                if (f != null)
                {
                    f.Close();
                }
                log.StatusMessage = "CustomUIHandler finished";
            }
        }
    }
}
