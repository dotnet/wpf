// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Test.Execution.Logging;
using Microsoft.Test.Execution.EngineCommands;

namespace Microsoft.Test.Execution
{
    /// <summary>
    /// Uses Windows Event logger to store infrastructure application events from Infra and ES. 
    /// Collects events, saves to disk and clears after a successful run.
    /// This provides a continuous log of machine specific infra activity - these logs should be tuned to contain actionable data.
    /// </summary>
    internal class ExecutionEventLog: ICleanableCommand
    {
        private static string qualityVaultSource = "QualityVault";

	    //We register ES as a source on behalf of ES, rather than native code registry muckery
        private static string elevationServiceSource = "ElevationService";

        //private static string elevationServiceSource = "ElevationService";
        private static string logName = "WPF Test Infrastructure";

        private DirectoryInfo LogFilesPath;

        internal static EngineCommands.ICleanableCommand Apply(DirectoryInfo directoryInfo, bool reset)
        {
            return new ExecutionEventLog(directoryInfo, reset);
        }

        public void Cleanup()
        {
            ExecutionEventLog.ConvertSystemEventsToReport(LogFilesPath);
        }

        ExecutionEventLog(DirectoryInfo logFilesPath, bool reset)
        {
            LogFilesPath = logFilesPath;
            if (reset)
            {
                //Infra is already registered as a source of event logs 
                //a previous run's data may be present, or a source may be registered on a different log.
                PurgeSource(elevationServiceSource);
                PurgeSource(qualityVaultSource);

                EventLog.CreateEventSource(qualityVaultSource, logName);
                EventLog.CreateEventSource(elevationServiceSource, logName);

                //TODO: Need to prepare policy on size/lifespan of events... 
                //If size controls are difficult, we can use non-default policy of oldest ones can be overwritten if needed(most recent is most important)
                ConfigureLog();
            }
        }

        private static void ConfigureLog()
        {
            EventLog log = new EventLog(logName);
            //Retain a generous 64mb in the log(must be  in increments of 64k). We delete the log after collecting results.            
            log.MaximumKilobytes = 64000;   
            //Delete oldest log data where neccessary.
            log.ModifyOverflowPolicy(OverflowAction.OverwriteAsNeeded, 0);
        }

        /// <summary>
        /// Record useful information to log.
        /// </summary>
        /// <param name="message"></param>
        static internal void RecordStatus(string message)
        {            
            RecordEvent(message, EventLogEntryType.Information, false);
        }

        /// <summary>
        /// Record less information to log, but not to EventLog
        /// </summary>
        /// <param name="message"></param>
        static internal void RecordVerboseStatus(string message)
        {
            RecordEvent(message, EventLogEntryType.Information, true);
        }

        /// <summary>
        /// Record exception to log.
        /// </summary>
        /// <param name="e"></param>
        static internal void RecordException(Exception e)
        {
            RecordEvent("Execution error due:" + e.ToString(), EventLogEntryType.Error, false);
        }        

        /// <summary>
        /// Extracts System events from OS for reporting.
        /// </summary>
        static internal void ConvertSystemEventsToReport(DirectoryInfo logPath)
        {                       
            ExecutionEventReportGenerator.Generate(new EventLog(logName).Entries, logPath);                       
        }

        /// <summary>
        /// Writes event to windows event log, and broadcasts it to be traced.
        /// </summary>        
        static private void RecordEvent(string message, EventLogEntryType eventType, bool isVerbose)
        {
            LoggingMediator.LogEvent(string.Format("QualityVault: {0}", message));
            if (!isVerbose)
            {
                EventLog.WriteEntry(qualityVaultSource, message, eventType); //Note: Infra does a lot of trivial logging stuff...
            }
        }


        /// <summary>
        /// Removes a pre-existing Windows Event Log Source
        /// </summary>
        /// <param name="source"></param>
        private static void PurgeSource(string source)
        {            
                if (EventLog.SourceExists(source))
                {
                    try
                    {
                        string oldLogName = EventLog.LogNameFromSourceName(source, ".");
                        EventLog.DeleteEventSource(source);
                        if (oldLogName != null)
                        {
                            EventLog.Delete(oldLogName);
                        }
                    }
                    catch (Exception)
                    {
                        //HACK: No-op - The EventLog Deletion API is unreliable, and a successful query does not guarantee the presence of the log.
                    }
            }
            
        }
    }
}