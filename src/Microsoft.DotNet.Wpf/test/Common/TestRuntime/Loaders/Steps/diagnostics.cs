// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Diagnostics;
using System.Security;
using System.Security.Permissions;

namespace Microsoft.Test.Utilities
{
    [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Assert, Unrestricted = true)]
    internal class ProcessUtils
    {
        /// <summary>
        /// ExecuteProcess wraps the creation and execution of a process
        /// </summary>
        /// <param name="Command"></param>
        /// <param name="Args"></param>
        /// <param name="useShellExecute"></param>
        /// <param name="waitForExit"></param>
        /// <param name="exitCode"></param>
        /// <param name="stdout"></param>
        /// <param name="ProcId"></param>
        static internal void ExecuteProcess(string Command, string Args, bool useShellExecute, bool waitForExit, out int exitCode, out string stdout, out int ProcId)
        {
            Process ssProc = null;
            ProcId = 0;

            exitCode = 0;
            stdout = String.Empty;
            try
            {
                ssProc = new Process();

                ProcessStartInfo ssProcSI = new ProcessStartInfo();

                // setup the ProcessInfo structure
                ssProcSI.FileName = Command;
                ssProcSI.Arguments = Args;
                ssProcSI.UseShellExecute = useShellExecute;

                if (!useShellExecute)
                {
                    ssProcSI.RedirectStandardOutput = true;
                    ssProcSI.RedirectStandardError = true;
                }
                ssProcSI.WorkingDirectory = "";
                ssProc.StartInfo = ssProcSI;

                // start the process
                ssProc.Start();

                // fill the proc id
                ProcId = ssProc.Id;

                // read the command output
                if (!useShellExecute)
                {
                    stdout = ssProc.StandardOutput.ReadToEnd();
                }

                // wait the exe for exit
                if (waitForExit)
                {
                    ssProc.WaitForExit();
                    exitCode = ssProc.ExitCode;
                }
            }
            finally
            {
                if (ssProc != null)
                {
                    ssProc.Dispose();
                }
            }
        }

        /// <summary>
        /// GetCallStack returns a string array with the names of methods in the frames from which this method is called
        /// (this method is not included in the list)
        /// </summary>
        /// <returns>a string array</returns>
        static internal string[] GetCallStack()
        {
            // get the stack trace
            StackTrace stack = new StackTrace();
            string[] frames = new string[stack.FrameCount - 1];

            // get frames
            int numberOfFrame = 0;

            bool firstFrame = true;
            foreach (StackFrame frame in stack.GetFrames())
            {
                if (firstFrame)
                {
                    // jump over the first frame (this method)
                    firstFrame = false;
                    continue;
                }

                // add the frame to the list
                frames[numberOfFrame] = frame.GetMethod().ToString();
                numberOfFrame++;
            }

            // return the frames
            return (frames);
        }

        /// <summary>
        /// KillProcess
        /// </summary>
        /// <param name="targetMainModuleName"></param>
        internal static void KillProcess(string targetMainModuleName)
        {
            foreach (Process p in Process.GetProcesses())
            {
                // get the main module name
                string mainModuleName = null;
                try
                {
                    mainModuleName = p.MainModule.ModuleName;
                }
                catch (NullReferenceException)
                {
                    // make sure main module is not null (for example, 'System' process has a null main module
                    continue;
                }
                catch (Win32Exception)
                {
                    // for some processes, it throws "System.ComponentModel.Win32Exception: Unable to enumerate the process modules."
                    continue;
                }

                // if found, kill it
                if (String.Compare(mainModuleName, targetMainModuleName, true, CultureInfo.InvariantCulture) == 0)
                {
                    // found
                    p.Kill();
                }
            }
        }

        /// <summary>
        /// IsProcessRunning
        /// </summary>
        /// <param name="targetMainModuleName">name of the exe/dll to look for (i.e. 'winlogon.exe')</param>
        /// <returns></returns>
        internal static bool IsProcessRunning(string targetMainModuleName)
        {
            foreach (Process p in Process.GetProcesses())
            {
                // get the main module name
                string mainModuleName = null;
                try
                {
                    mainModuleName = p.MainModule.ModuleName;
                }
                catch (NullReferenceException)
                {
                    // make sure main module is not null (for example, 'System' process has a null main module
                    continue;
                }
                catch (Win32Exception)
                {
                    // for some processes, it throws "System.ComponentModel.Win32Exception: Unable to enumerate the process modules."
                    continue;
                }

                // if found, kill it
                if (String.Compare(mainModuleName, targetMainModuleName, true, CultureInfo.InvariantCulture) == 0)
                {
                    // found
                    return (true);
                }
            }

            // not found
            return (false);
        }
    }

    /// <summary>
    /// Encapsulates common functionality for getting process info
    /// </summary>
    internal class ProcessInfo
    {
        internal static string GetAssemblyLocation(Assembly asm)
        {
            // assert file io permissions required for getting the assembly location
            (new FileIOPermission(PermissionState.Unrestricted)).Assert();

            // get the location
            string location = asm.Location;

            // revert the asserted permissions
            FileIOPermission.RevertAssert();

            // return the location
            return (location);
        }
    }
}
