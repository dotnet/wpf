// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Test.Execution.EngineCommands;
using Microsoft.Test.Logging;

namespace Microsoft.Test.Execution.Logging
{
    /// <summary>
    /// Mediates relationship between logging server and Execution Engine.
    /// </summary>
    internal class LoggingMediator
    {
        #region Members
        private LoggingServer server;

        private RecordingLogger recordingLogger;
        private ConsoleLogger consoleLogger;
        private ProgressWindowLogger windowProgressLogger;

        private static Stack<ExecutionGroupLogCommand> executionLoggers = new Stack<ExecutionGroupLogCommand>();// EVIL, EVIL HACK: This guy shouldn't be static.
        private static bool hasTests = false;

        private bool debugTests;

        #endregion

        public LoggingMediator(bool debugTests)
        {
            this.debugTests = debugTests;            
        }
        
        internal void StartService(DebuggingEngineCommand debuggingEngine, int testCount)
        {
            ExecutionEventLog.RecordStatus("Starting up LoggingMediator.");
            server = new LoggingServer(debuggingEngine, debugTests);
            server.Start();
            consoleLogger = new ConsoleLogger();
            recordingLogger = new RecordingLogger();
            windowProgressLogger = new ProgressWindowLogger(testCount);

            server.RegisterLogger(recordingLogger);
            server.RegisterLogger(consoleLogger);
            server.RegisterLogger(windowProgressLogger);
        }

        internal void StartTests(List<TestRecord> tests, DirectoryInfo testLogDirectory)
        {
            hasTests = true;
            recordingLogger.RegisterTests(tests);
            server.LoggingNormalizer.RegisterTests(tests, testLogDirectory);
        }

        internal void EndTests(List<TestRecord> tests, bool waitForTestToTerminate)
        {
            int repeatLimit = 10; // arbitrary
            while (waitForTestToTerminate && server.LoggingNormalizer.IsListening && repeatLimit > 0)
            {
                Thread.Sleep(100);
                repeatLimit--;
            }
            server.LoggingNormalizer.UnregisterTests();
            hasTests = false;
        }

        internal void StopService()
        {            
            server.Stop();
            windowProgressLogger.Close();
        }

        static internal void LogEvent(string message)
        {
            LoggingServer service=LoggingServer.Instance;
            if (service != null)
            {
                service.LoggingNormalizer.LogMessage(message);
            }

            if(!hasTests && executionLoggers.Count > 0)
            {
                ExecutionGroupLogCommand groupLogger = executionLoggers.Peek();
                if (groupLogger != null)
                {
                    groupLogger.LogEvent(message);
                }
            }
        }

        static internal void LogFile(string filename)
        {            
            LoggingServer service = LoggingServer.Instance;
            if (service != null)
            {
                service.LoggingNormalizer.LogFile(filename);
            }

            if (!hasTests && executionLoggers.Count > 0)
            {
                ExecutionGroupLogCommand groupLogger = executionLoggers.Peek();
                if (groupLogger != null)
                {
                    groupLogger.LogFile(filename);
                }
            }
            
        }

        /// <summary>
        /// Notifies Mediator to pop a Logging subscriber from the stack - Caller is trusted to know what it is doing- no validation here.
        /// </summary>        
        internal void PopListener()
        {
            executionLoggers.Pop();
        }

        /// <summary>
        /// Notifies Mediator to push a Logging subscriber onto the stack
        /// </summary>    
        internal void PushListener(ExecutionGroupLogCommand command)
        {
            executionLoggers.Push(command);
        }

        internal IEnumerable<FileInfo> ExecutionLogFiles
        {
            get
            {
                return executionLoggers.ToArray().Select(executionGroupLogCommand => executionGroupLogCommand.LogFileLocation);
            }
        }
    }
}
