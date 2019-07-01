// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Security.Permissions;
using System.Threading;
using Microsoft.Test.Execution.EngineCommands;
using Microsoft.Test.Logging;

namespace Microsoft.Test.Execution.Logging
{
    /// <summary>
    /// The LoggingServer is the receiver of communications from the Driver's LoggingClient.
    /// 
    /// Note: an accessLock object ensures all incoming communication is processed in series.
    /// </summary>    
    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
    internal class LoggingServer : ILogger
    {
        #region Constructors/Singleton Accessors
        /// <summary/>
        public LoggingServer(DebuggingEngineCommand debuggingEngine, bool debugTests)
        {
            LoggingNormalizer = new LoggingNormalizer(debuggingEngine, debugTests);
            Instance = this;
        }

        #endregion

        #region Public Members

        internal void RegisterLogger(ILogger logger)
        {
            this.LoggingNormalizer.Loggers.Add(logger);
        }

        /// <summary>
        /// Starts the LoggingService's ServiceHost.  This must be called before any clients attempt to connect.
        /// </summary>
        public void Start()
        {
            logCancellationToken = LogContract.RegisterServer(this);
        }

        /// <summary>
        /// Stops the logging service.
        /// </summary>
        public void Stop()
        {
            LogContract.UnRegisterServer(logCancellationToken);
        }

        /// <summary>
        /// TestLogDataNormalizer responsible for keeping track of state and
        /// interpreting TestLogData traces such that state constraints are maintained.
        /// </summary>
        public LoggingNormalizer LoggingNormalizer { get; set; }

        #endregion

        #region Private fields

        private CancellationTokenSource logCancellationToken;
        public static LoggingServer Instance { get; set; }
        private static object accessLock = new object();

        #endregion

        #region ILogger Members

        public void BeginTest(string testName)
        {
            lock (accessLock)
            {
                LoggingNormalizer.BeginTest(testName);
            }
        }

        public void BeginVariation(string variationName)
        {
            lock (accessLock)
            {
                LoggingNormalizer.BeginVariation(variationName);
            }
        }

        public void EndTest(string testName)
        {
            lock (accessLock)
            {
                LoggingNormalizer.EndTest(testName);
            }
        }

        public void EndVariation(string variationName)
        {
            lock (accessLock)
            {
                LoggingNormalizer.EndVariation(variationName);
            }
        }

        public void LogFile(string filename)
        {
            lock (accessLock)
            {
                LoggingNormalizer.LogFile(filename);
            }
        }

        public void LogMessage(string message)
        {
            lock (accessLock)
            {
                LoggingNormalizer.LogMessage(message);
            }
        }

        public void LogObject(object payload)
        {
            lock (accessLock)
            {
                LoggingNormalizer.LogObject(payload);
            }
        }

        public void LogProcess(int ProcessId)
        {
            lock (accessLock)
            {
                LoggingNormalizer.LogProcess(ProcessId);
            }
        }

        public void LogProcessCrash(int processId)
        {
            lock (accessLock)
            {
                LoggingNormalizer.LogProcessCrash(processId);
            }
        }

        public void LogResult(Result result)
        {
            lock (accessLock)
            {
                LoggingNormalizer.LogResult(result);
            }
        }

        public string GetCurrentTestName()
        {
            lock (accessLock)
            {
                return LoggingNormalizer.GetCurrentTestName();
            }
        }

        public string GetCurrentVariationName()
        {
            lock (accessLock)
            {
                return LoggingNormalizer.GetCurrentVariationName();
            }
        }

        public Result? GetCurrentVariationResult()
        {
            lock (accessLock)
            {
                return LoggingNormalizer.GetCurrentVariationResult();
            }
        }

        public int? GetCurrentTestLogResult()
        {
            lock (accessLock)
            {
                return LoggingNormalizer.GetCurrentTestLogResult();
            }
        }

        public void SetCurrentTestLogResult(int result)
        {
            lock (accessLock)
            {
                LoggingNormalizer.SetCurrentTestLogResult(result);
            }
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
