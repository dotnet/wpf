// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Principal;
using Microsoft.Test.Execution.Logging;

namespace Microsoft.Test.Execution.EngineCommands
{
    /// <summary>
    /// Manages automatic debugging of test process/crashes
    /// -Contains policy/contracts with regards command line to invocation of QVD
    /// -Interprets and process crash event messages produced from QVD->Logging->LoggingMediator
    /// -Logs debugging results for users to review
    /// </summary>    
    public class DebuggingEngineCommand : ICleanableCommand
    {
        #region Private Fields

        private static readonly string debuggerArgument = " /PID=%ld";

        private List<int> pids = new List<int>();
        private DirectoryInfo executionDirectory;
        private bool isTestRunning { get { return (executionDirectory != null); } }

        //Represent accumulated cost of dumps. Stop dumping after we exceed maximum.
        private long accumulatedDumpCost;
        private const long maxDumpCost = 2000000000;

        #endregion

        private DebuggingEngineCommand() { }

        /// <summary>
        /// Registers Jit Debugger rooted from supplied infra binaries Path and returns cleanup command.
        /// </summary>
        public static DebuggingEngineCommand Apply(DirectoryInfo infraPath, string customJitDebuggerCommand)
        {
            ExecutionEventLog.RecordStatus("Starting up DebuggingEngine.");
            DebuggingEngineCommand command = new DebuggingEngineCommand();
            CdbUtilities.InstallCdb();
            if (!String.IsNullOrEmpty(customJitDebuggerCommand))
            {
                JitRegistrationUtilities.Register(customJitDebuggerCommand);
            }
            else
            {
                JitRegistrationUtilities.Register(GenerateJitDebuggerCommand(infraPath));
            }
            return command;
        }

        /// <summary/>        
        public void Cleanup()
        {
            Rollback();
        }

        /// <summary>
        /// Provides stateless cleanup without having to run full infra cycle.
        /// </summary>
        public static void Rollback()
        {
            JitRegistrationUtilities.Unregister();
        }

        #region Test Status API

        internal void TestStarted(int pid, DirectoryInfo executionDirectory)
        {
            ExecutionEventLog.RecordStatus("Main Test Process "+pid+" has started.");
            pids.Add(pid);
            this.executionDirectory = executionDirectory;            
        }

        /// <summary>
        /// Adds process to list of process under test
        /// </summary>        
        internal void OnProcessRegister(int processId)
        {
            //Only register processes when the test has started.
            if (isTestRunning)
            {
                pids.Add(processId);
                ExecutionEventLog.RecordStatus("Process: "+processId+" now marked for tracking.");
            }
        }

        /// <summary>
        /// Gather debug information if the process was being monitored, otherwise just terminate.
        /// </summary>
        internal void OnProcessCrash(int processId)
        {
            ExecutionEventLog.RecordStatus("Process " + processId + " crash notification recieved.");
            try
            {
                if (Process.GetCurrentProcess().Id == processId)
                {
                    //It may be possible that some strange unhandled exception is arising in a background thread, leading to a debugger being launched
                    //and the infra Terminating itself. Somehow, it seems unlikely for this code to be reachable, but this case is a hypothetical possibility to account for.
                    ExecutionEventLog.RecordStatus("Catastrophic Situation: Infra has recieved notification that it's own process has crashed. ");
                }
                else if (isTestRunning && pids.Contains(processId) && accumulatedDumpCost < maxDumpCost)
                {
                    ExecutionEventLog.RecordStatus("Debugging Process: " + processId);
                    FileInfo debugLogFilePath = new FileInfo(Path.Combine(executionDirectory.FullName, "TestInfraDebuggingLog_" + processId + ".log"));
                    FileInfo debugDumpFilePath = new FileInfo(Path.Combine(executionDirectory.FullName, "TestInfraDebuggingDump_" + processId + ".dmp"));

                    CdbUtilities.DebugProcess(processId.ToString(CultureInfo.InvariantCulture), debugLogFilePath, debugDumpFilePath);

                    accumulatedDumpCost += debugDumpFilePath.Length;
                    //TODO: Apply File Compression

                    LoggingMediator.LogFile(debugLogFilePath.FullName);
                    LoggingMediator.LogFile(debugDumpFilePath.FullName);
                    //Dan: Is the Recording Logger guaranteed to be explicitly aware that the given test has failed if this point is reached?
                    //Getting here means a process the tester cares about has crashed. 
                }
                else
                {
                    if(accumulatedDumpCost < maxDumpCost)
                    {
                        ExecutionEventLog.RecordStatus("Terminating non-monitored Process:" + processId);
                    }
                    else
                    {
                        ExecutionEventLog.RecordStatus("Dump limit exceeded - Terminating process without analysis:" + processId);
                    }
                    Process process = Process.GetProcessById(processId);
                    if (process != null && !process.HasExited)
                    {
                        process.Kill();
                        process.WaitForExit();
                    }
                }
            }
            //Uncaught exceptions in this event handler will blow up the logging stack... Which would be bad.
            catch (Exception exception)
            {
                ExecutionEventLog.RecordException(exception);
            }
        }

        /// <summary>
        /// Cleans up process state after test has ended.
        /// </summary>
        internal void TestEnded(DateTime startTime)
        {
            pids.Clear();
            ExecutionEventLog.RecordStatus("Now cleaning up processes.");
            TerminateProcesses(IdentifyUnwantedProcesses(startTime));
            ReportRogueProcesses(IdentifyUnwantedProcesses(startTime));            
            executionDirectory = null;
        }

        private void ReportRogueProcesses(List<Process> list)
        {
            foreach (Process p in list)
            {
                try
                {
                    ExecutionEventLog.RecordStatus("ALERT- Possible rogue process! Unsuccessfully terminated:" + p.ProcessName + " " + p.Id);
                }
                catch (Exception e)
                {
                    ExecutionEventLog.RecordException(e);
                }
            }
        }

        #endregion

        #region Private Implementation

        /// <summary>
        /// Encapsulates termination of processes.
        /// Debug processes are not a problem, as they execute synchronously.
        /// </summary>
        /// <param name="processes"></param>
        private void TerminateProcesses(List<Process> processes)
        {
            foreach (Process process in processes)
            {
                if (!process.HasExited)
                {
                    try
                    {
                        ExecutionEventLog.RecordStatus("Process Name: " + process.ProcessName + " ID:" + process.Id + " is being terminated.");
                        process.Kill();
                        process.WaitForExit();
                        ExecutionEventLog.RecordStatus("Process termination completed.");
                    }
                    catch (InvalidOperationException e)
                    {
                        ExecutionEventLog.RecordException(e);
                        //The process has already exited
                    }
                    catch (Win32Exception e)
                    {
                        ExecutionEventLog.RecordException(e);
                        //The process has already exited or access is denied (Probably a Whidbey bug)
                    }
                    catch (Exception e)
                    {
                        ExecutionEventLog.RecordException(e);
                    }
                    finally
                    {
                        if(!process.HasExited)
                        {
                            ExecutionEventLog.RecordStatus("Could not kill process " + process.ProcessName + " ID:" + process.Id);
                            ExecutionEventLog.RecordStatus("Launching external taskkill process to retry killing...");
                            KillWithMoreForce(process);
                        }
                    }
                }
            }
        }

        private void KillWithMoreForce(Process process)
        {
            ProcessUtilities.Run("taskkill.exe", "/f /pid " + process.Id.ToString());
        }

        /// <summary>
        /// Encapsulates discovering processes which emerged during the test run        
        /// </summary>        
        /// <returns></returns>
        private List<Process> IdentifyUnwantedProcesses(DateTime startTime)
        {
            string userSid = WindowsIdentity.GetCurrent().User.Value;

            List<Process> newProcesses = new List<Process>();
            foreach (Process process in Process.GetProcesses())
            {
                try
                {
                    if (IsUnwantedProcess(startTime, userSid, process))
                    {
                        newProcesses.Add(process);
                    }
                }
                catch (Exception e)
                {
                    //With filtering, we shouldn't be hitting this. -TODO: Remove if it isn't hit during runs.
                    ExecutionEventLog.RecordException(e); 
                }
            }
            return newProcesses;
        }

        /// <summary>
        /// Determines if a process should not be kept around after test execution
        /// </summary>
        /// <returns>whether this process is unwanted</returns>
        private static bool IsUnwantedProcess(DateTime startTime, string userSid, Process process)
        {
            // Don't shutdown a critical process or the developers IDE
            if (ProcessUtilities.IsCriticalProcess(process) || ProcessUtilities.IsIDE(process))
            {
                return false;
            }

            // Shutdown any process started within the span of the test. This seems overly aggressive but probably has good reason.
            if (userSid.Equals(ProcessUtilities.GetProcessUserSid(process), StringComparison.InvariantCultureIgnoreCase) && !process.HasExited && process.StartTime > startTime))
            {
                return true;
            }

            // Shutdown any known processes associated with tests
            if (ProcessUtilities.IsKnownTestProcess(process))
            {
                return true;
            }
            
            return false;
        }        

        private static string GenerateJitDebuggerCommand(DirectoryInfo infraPath)
        {
            string debuggerPath = Path.Combine(infraPath.FullName, @"QualityVaultDebugger.exe");
            if (!Path.IsPathRooted(debuggerPath) || !File.Exists(debuggerPath))
            {
                throw new FileNotFoundException(string.Format(CultureInfo.InvariantCulture, "Could not locate JitDebugger executable: '{0}'.", debuggerPath));
            }
            return debuggerPath + " " + debuggerArgument;
        }

        #endregion        
    }
}
