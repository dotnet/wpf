// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;

namespace Microsoft.Test.Execution.EngineCommands
{
    /// <summary>
    /// Implements service control usage patterns and contains error handling policy.
    /// This is not a thread safe API - More robustness for failure needed here.    
    /// </summary>
    public class ElevationServiceCommand : ICleanableCommand
    {
        /// <summary>
        /// Sets up a controller of the elevation service
        /// Contains policy about where the exe lives... This and CleanupCommand need to be refactored to push policy up
        /// </summary>
        public static ElevationServiceCommand Apply(DirectoryInfo infraBinariesPath)
        {
            ElevationServiceCommand command =new ElevationServiceCommand();
            ExecutionEventLog.RecordStatus("Starting Elevation Service.");
            command.rootPath = Path.Combine(infraBinariesPath.FullName, @"ElevationService.exe");
            //Obliterate previous ES 
            CallElevationService(command.rootPath, "forceUninstall");

            //Engage the Elevation Service
            CallElevationService(command.rootPath, "install");
            CallElevationService(command.rootPath, "start");
            return command;
        }

        /// <summary>
        /// Cleans up the controller for the elevation service
        /// </summary>
        public void Cleanup()
        {
            ExecutionEventLog.RecordStatus("Removing Elevation Service.");
            RemoveInstallation(rootPath);            
        }

        /// <summary>
        /// Efficiently removes the elevation service, regardless of state.
        /// </summary>
        public static void RemoveInstallation(string rootPath)
        {
            //Disengage the Elevation Service
            CallElevationService(rootPath, "forceUninstall");
        }

        //ES should simply not fail - any deviation from this needs to be promptly resolved. 
        private static void CallElevationService(string rootPath, string command)
        {
            //Move to ProcessUtilities.
            ProcessStartInfo startInfo = new ProcessStartInfo(rootPath, command);
            startInfo.RedirectStandardOutput = true;
            startInfo.UseShellExecute = false;
            Trace.TraceInformation("Running Elevation Service Command:" + command);
            Process p = Process.Start(startInfo);

            p.WaitForExit();                       


            if (p.ExitCode != 0)
            {
                throw new InvalidOperationException("Elevation Service Failed: " + p.StandardOutput.ReadToEnd());
            }
        }

        private string rootPath;
    }
}