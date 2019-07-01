// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Test.Execution.EngineCommands;
using Microsoft.Test.Execution.StateManagement.ModernShell;

namespace Microsoft.Test.Execution
{
    /// <summary>
    /// Encapsulates work of launching test drivers in normal and debug conditions.
    /// </summary>
    internal static class DriverLauncher
    {
        /// <summary>
        /// Runs Test process. Returns duration of execution.
        /// </summary>
        internal static TimeSpan Launch(ExecutionSettings settings, List<TestRecord> tests, DirectoryInfo executionDirectory, DebuggingEngineCommand debuggingEngine)
        {
            //Small hack to lessen the chance that Mosh UI on Win8 will interfere with tests.
            //This can be removed once all tests play nice and don't bring up Mosh in the middle of the runs.
            if (Environment.OSVersion.Version.Major >= 6 && Environment.OSVersion.Version.Minor >= 2)
            {
                if (ModernShellUtilities.IsImmersiveWindowOpen()) ModernShellUtilities.EnsureDesktop();
            }
            PrepareDriverPayload(tests, executionDirectory);
            //Hmm. Need to check for overflow, or be less Linq-ish...
            int timeout = AddUpTimeout(settings, tests);

            DriverLaunchSettings.StoreSettings(executionDirectory.FullName, settings.TestBinariesDirectory.FullName);
            ProcessStartInfo startInfo = PrepareProcessStartInfo(tests.First().TestInfo.Driver.Executable, executionDirectory, settings);
            ExecutionEventLog.RecordStatus("Running Driver sandbox.");
            Process p = Process.Start(startInfo);
            debuggingEngine.TestStarted(p.Id, executionDirectory);

            int millisecondsTimeout = timeout;
            if (!p.WaitForExit(millisecondsTimeout))
            {
                TerminateLaggard(p, millisecondsTimeout);
            }

            debuggingEngine.TestEnded(p.StartTime);

            return (p.ExitTime - p.StartTime);
        }

        #region Private Members

        private static int AddUpTimeout(ExecutionSettings settings, List<TestRecord> tests)
        {
            int total = 0;
            foreach (TestRecord test in tests)
            {
                int timeout = ConvertTimeoutToMilliSeconds(settings.DetermineTimeout(test.TestInfo.Timeout, test.TestInfo.Type));
                if (timeout >= int.MaxValue)
                {
                    return int.MaxValue;
                }
                total += timeout;
            }
            return total;
        }

        private static int ConvertTimeoutToMilliSeconds(TimeSpan timeout)
        {
            int convertedValue;
            if (timeout.TotalMilliseconds >= int.MaxValue)
            {
                convertedValue = int.MaxValue;
            }
            else
            {
                convertedValue = (int)timeout.TotalMilliseconds;
            }
            return convertedValue;
        }

        private static void TerminateLaggard(Process p, int millisecondsTimeout)
        {
            ExecutionEventLog.RecordStatus(
                "Test did not terminate before execution timeout limit of " + (millisecondsTimeout / 1000) +
                " seconds. Terminating: " + p.ProcessName +
                " now. This limit is controlled from TestInfo.Timeout."
                );
            p.Kill();
        }

        /// <summary>
        /// Prepares start information for running tests via shell execution.
        /// Contains environment var contract for TestBinRoot and ExecutionDirectory
        /// </summary>
        private static ProcessStartInfo PrepareProcessStartInfo(string driver, DirectoryInfo executionDirectory, ExecutionSettings settings)
        {
            string commandSeparator = " & ";
            string quotedCommandSeparator = " ^& ";

            string arguments = String.Empty;
            string instructions0 = "NOTE: You can pass the /waitForDebugger flag to rundrts.cmd to have tests automatically wait to attach!";
            string instructions1 = "Run: VS.cmd, rascal.cmd, windbg.cmd or cdb.cmd to debug your test. ";
            string instructions2 = "Go to http://ddrelqa/dogfoodtracker/ to install Rascal Pro.";
            string instructions3 = "Close this console when you are done.";
            if (settings.DebugTests && !settings.WaitForDebugger)
            {
            	string programFiles = System.Environment.Is64BitOperatingSystem ? "%ProgramFiles(x86)%" : "%ProgramFiles%";
				string vs1 = programFiles + @"\Microsoft Visual Studio\";
				string vs2a = @"..\IDE\devenv.exe";						// for VS2015-
				string vs2b = @"\Enterprise\Common7\IDE\devenv.exe";	// for VS2017+
                string vs2c = @"\Preview\Common7\IDE\devenv.exe";	// for VS Previews
                string vscmd = "@setlocal enabledelayedexpansion"
                    + quotedCommandSeparator
                    + "@set devenvCmd=@echo VS not found"
                    + quotedCommandSeparator
                    + "(@for /f \"tokens=2 delims==\" %%I in ('set vs') do "
                    + "@if exist \"%%I" + vs2a + "\" set devenvCmd=\"%%I" + vs2a + "\" /debugexe)"
                    + quotedCommandSeparator
                    + "(@for /d %%I in (\"" + vs1 + "*\") do "
                    + "@if exist \"%%I" + vs2b + "\" set devenvCmd=\"%%I" + vs2b + "\" /debugexe)"
                    + quotedCommandSeparator
                    + "(@for /d %%I in (\"" + vs1 + "*\") do "
                    + "@if exist \"%%I" + vs2c + "\" set devenvCmd=\"%%I" + vs2c + "\" /debugexe)"
                    + quotedCommandSeparator
                    + "@echo !devenvCmd! " + driver
                    + quotedCommandSeparator
                    + "!devenvCmd! ";
                string rascalPath = @"%SystemDrive%\RascalPro\Common7\IDE\";
                //Provide an interactive console which remains up, listing debug commands for user
                arguments = "/K title " + instructions1 + commandSeparator
                    + " echo " + instructions0 + commandSeparator
                    + " echo " + instructions1 + commandSeparator
                    + " echo " + instructions2 + commandSeparator
                    + " echo " + instructions3 + commandSeparator

                    + MakeDebuggerCommand("VS.cmd", vscmd, driver) + commandSeparator
                    + MakeDebuggerCommand("VS9.cmd", "\"%VS90COMNTOOLS%/" + vs2a + "\" /debugexe ", driver) + commandSeparator
                    + MakeDebuggerCommand("VS10.cmd", "\"%VS100COMNTOOLS%/" + vs2a + "\" /debugexe ", driver) + commandSeparator
                    + MakeDebuggerCommand("VS11.cmd", "\"%VS110COMNTOOLS%/" + vs2a + "\" /debugexe ", driver) + commandSeparator
                    + MakeDebuggerCommand("VS12.cmd", "\"%VS120COMNTOOLS%/" + vs2a + "\" /debugexe ", driver) + commandSeparator
                    + MakeDebuggerCommand("VS13.cmd", "\"%VS130COMNTOOLS%/" + vs2a + "\" /debugexe ", driver) + commandSeparator
                    + MakeDebuggerCommand("VS14.cmd", "\"%VS140COMNTOOLS%/" + vs2a + "\" /debugexe ", driver) + commandSeparator
                    + MakeDebuggerCommand("VS2017.cmd", "\"" + vs1 + "2017" + vs2b + "\" /debugexe ", driver) + commandSeparator
                    + MakeDebuggerCommand("VS2018.cmd", "\"" + vs1 + "2018" + vs2b + "\" /debugexe ", driver) + commandSeparator
                    + MakeDebuggerCommand("VS2019.cmd", "\"" + vs1 + "2019" + vs2b + "\" /debugexe ", driver) + commandSeparator
                    + MakeDebuggerCommand("VS2020.cmd", "\"" + vs1 + "2020" + vs2b + "\" /debugexe ", driver) + commandSeparator
                    + MakeDebuggerCommand("VS2021.cmd", "\"" + vs1 + "2021" + vs2b + "\" /debugexe ", driver) + commandSeparator
                    + MakeDebuggerCommand("VS2022.cmd", "\"" + vs1 + "2022" + vs2b + "\" /debugexe ", driver) + commandSeparator
                    + MakeDebuggerCommand("VS2023.cmd", "\"" + vs1 + "2023" + vs2b + "\" /debugexe ", driver) + commandSeparator
                    + MakeDebuggerCommand("VS2024.cmd", "\"" + vs1 + "2024" + vs2b + "\" /debugexe ", driver) + commandSeparator
                    + MakeDebuggerCommand("CDB.cmd", "\"c:/debuggers/cdb.exe\" ", driver) + commandSeparator
                    + MakeDebuggerCommand("WINDBG.cmd", "\"c:/debuggers/windbg.exe\" ", driver) + commandSeparator
                    + MakeDebuggerCommand("WINDBGX.cmd", "\"%localappdata%/dbg/ui/windbgX.exe\" ", driver) + commandSeparator
                    + MakeDebuggerCommand("Rascal.cmd", rascalPath + "RascalPro.exe /debugexe ", driver);
            }
            else
            {
                //Provide a console for running the driver
                arguments = "/C " + driver;
            }

            if (settings.WaitForDebugger)
            {
                arguments += " /wait";
            }
            
            if (settings.DebugSti)
            {
                arguments += " /debugsti";
            }

            ProcessStartInfo startInfo = ProcessUtilities.CreateStartInfo("cmd.exe", arguments, ProcessWindowStyle.Normal);

            startInfo.WorkingDirectory = executionDirectory.FullName;

            return startInfo;
        }

        /// <summary>
        /// Creates a command file for running driver under a debugger
        /// </summary>
        private static string MakeDebuggerCommand(string name, string command, string driver)
        {
            return "echo " + command + driver + " > " + name;
        }

        private static void PrepareDriverPayload(List<TestRecord> tests, DirectoryInfo executionDirectory)
        {

            List<TestInfo> testInfos = new List<TestInfo>();
            foreach (TestRecord test in tests)
            {
                testInfos.Add(test.TestInfo);
            }
            string xml = ObjectSerializer.Serialize(testInfos);
            File.WriteAllText(Path.Combine(executionDirectory.FullName, "TestInfos.xml"), xml);
        }

        #endregion
    }
}
