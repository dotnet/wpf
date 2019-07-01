// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using Microsoft.Test.CommandLineParsing;
using Microsoft.Test.Logging;
using System.Diagnostics;

namespace Microsoft.Test.Execution.Debugging
{
    /// <summary>
    /// Stable entry Point for reporting crash events.     
    /// </summary>
    public static class QualityVaultDebugger
    {
        /// <summary>
        /// Provides an entry point for Crash Reporting tool.
        /// </summary>
        public static int Main(string[] args)
        {
            try
            {
                CommandLineDictionary dictionary = CommandLineDictionary.FromArguments(args);
                int pid = int.Parse(dictionary["PID"], CultureInfo.InvariantCulture);
                ReportCrash(pid);
            }
            //This process must not crash in event of failure
            //otherwise we risk recursive crashing sequence which could get ugly.
            catch (Exception e)
            {
                Log(e.ToString());
            }

            return 0;
        }


        //Dead simple logging to avoid any mode of failure
        internal static void Log(string message)
        {
            Console.WriteLine(message);
        }

        /// <summary>
        /// Signal to the infra, a process has crashed.
        /// </summary>
        /// <param name="pid"></param>
        internal static void ReportCrash(int pid)
        {
            LoggingClient remoteLoggingClient = LogContract.ConnectClient(); ;
            try
            {
                Log("Opening connection to Infrastructure to report crash of:" + pid);

                remoteLoggingClient.LogMessage("Process Crashed!");
                remoteLoggingClient.LogProcessCrash(pid);
            }
            catch (Exception)
            {
                Log("QualityVaultDebugger was unable to communicate with Infrastructure.");
            }

            finally
            {
                if (remoteLoggingClient != null)
                {
                    remoteLoggingClient.Close();
                }
            }
        }
    }
}