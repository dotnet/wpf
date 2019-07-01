// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.Test.Execution.EngineCommands;
using Microsoft.Test.Logging;

namespace Microsoft.Test.Execution.Logging
{
    /// <summary>
    /// Manages test logging state.
    /// </summary>
    /// <remarks>
    /// Manages test state as defined via ILogger calls. It will
    /// evaluate the ILogger call based upon current logging state, and either
    /// forward the ILogger call directly, or make a series of ILogger calls
    /// to keep logging in a valid state.
    /// Refer to Test\Infra\Docs\Logging_System-Stateful_policies.xlsx for a
    /// breakdown of the expected state behavior.
    /// </remarks>
    internal class LoggingNormalizer : ILogger
    {
        #region Private Data

        private string currentVariationName;
        private int currentVariationIndex;
        private Result? currentResult;
        private int? currentTestLogResult;
        private bool hasConnection;
        private bool hasReceivedBeginTest;
        private bool hasReceivedBeginVariation;
        private bool isDebugging = true;
        private List<TestRecord> registeredTests;
        private int currentTestIndex = -1;
        public LoggerCollection Loggers { get; set; }
        private DebuggingEngineCommand debuggingEngine;
        private DirectoryInfo testLogDirectory;

        #endregion

        #region Constructor

        public LoggingNormalizer(DebuggingEngineCommand debuggingEngine, bool debugTests)
        {
            this.isDebugging = debugTests;
            this.debuggingEngine = debuggingEngine;
            Loggers = new LoggerCollection();
        }

        #endregion

        #region Public Members

        public void RegisterTests(List<TestRecord> tests, DirectoryInfo testLogDirectory)
        {
            // If the LoggingState is not offline it means there is another
            // test which hasn't been unregistered yet. We'll unregister that
            // test, and log to the execution event log that badness happened.
            if (LoggingState != LoggingState.Offline)
            {
                ExecutionEventLog.RecordStatus("RegisterTest was called when there was already a registered test.");
                UnregisterTests();
            }

            registeredTests = tests;
            currentTestIndex = 0;
            currentVariationName = null;
            currentVariationIndex = -1;
            IsListening = true;
            hasConnection = false;
            hasReceivedBeginTest = false;
            hasReceivedBeginVariation = false;
            this.testLogDirectory = testLogDirectory;
        }


        // Note: In future this will only be getting run once, after driver is terminated! Semantics should likely shift towards execution considerations.
        public void UnregisterTests()
        {
            //wrap up any unresolved log state
            if (LoggingState == LoggingState.HasVariation)
            {
                LogMessage("Variation was not Closed.");
                LogResult(Result.Fail);
                EndVariation(currentVariationName);
                EndTest(currentTestName);
            }

            else if (LoggingState == LoggingState.IsConnected)
            {
                BeginVariation("Non-existent Variation");
                LogMessage("Test did not close its log.");
                LogResult(Result.Fail);
                EndVariation("Non-existent Variation");
                EndTest(currentTestName);
            }
            // Execution registered the test, causing LoggingState to become
            // initialized, but no logging messages were ever received.
            else if (LoggingState == LoggingState.Initialized && !hasReceivedBeginTest)
            {
                BeginTest(currentTestName);
                BeginVariation("Non-existent Test Log");
                LogMessage("No Communications were received by this test.");
                LogResult(Result.Fail);
                EndVariation("Non-existent Test Log");
                EndTest(currentTestName);
            }

            IsListening = false;
            hasConnection = false;
            this.testLogDirectory = null;
        }

        public bool IsListening { get; set; }

        #endregion

        #region ILogger Members

        public void BeginTest(string testName)
        {
            if (LoggingState == LoggingState.IsConnected)
            {
                BeginVariation("Two variations created.");
                LogMessage("Log consumer has attempted to call Begin Test Twice.");
                LogResult(Result.Fail);
                EndVariation("Two variations created.");
            }
            else if (LoggingState == LoggingState.HasVariation)
            {
                LogMessage("Log consumer has attempted to call Begin Test Twice.");
                LogResult(Result.Fail);
                EndVariation(currentVariationName);
            }
            else
            {
                hasConnection = true;
                // If BeginTest specifies a different test name we will record
                // a warning in the execution event log. We could be even more
                // aggressive and create a dead test for the registered test.
                if (!String.Equals(this.currentTestName, testName))
                {
                    // If we're in debugging mode and the test name matches the first
                    // registered test, then what happened is the driver was rerun.
                    // In this case we can recover by reseting the loggers.
                    if (isDebugging && String.Equals(testName, registeredTests[0].TestInfo.Name))
                    {
                        Reset();
                        Loggers.Reset();
                    }
                    else
                    {
                        ExecutionEventLog.RecordStatus(string.Format(CultureInfo.InvariantCulture, "BeginTest specified '{0}' but test name registered was '{1}'.", testName, this.currentTestName));
                        throw new Exception("This is super serious now that we have execution groups, we cannot recover safely");
                    }
                }
                Loggers.BeginTest(testName);

                hasReceivedBeginTest = true;
            }
        }

        public void BeginVariation(string variationName)
        {
            if (LoggingState == LoggingState.IsConnected) //Proper scenario
            {
                Loggers.BeginVariation(variationName);
                currentVariationName = variationName;
                currentVariationIndex++;
                hasReceivedBeginVariation = true;
            }
            else if (LoggingState == LoggingState.HasVariation) //We're in a bad state. Synthesize corrective sequence.
            {
                LogMessage("BUG: Test began a second variation without ending the previous one.");
                LogResult(Result.Fail);
                EndVariation(currentVariationName);
                BeginVariation(variationName);
            }
        }

        public void EndTest(string testName)
        {
            if (LoggingState == LoggingState.HasVariation)
            {
                LogResult(Result.Fail);
                LogMessage("This variation was not closed before End Test.");
                EndVariation(currentVariationName);
                hasConnection = false;
                currentVariationName = null;
            }
            if (LoggingState == LoggingState.IsConnected)
            {
                if (!hasReceivedBeginVariation)
                {
                    BeginVariation("Non-existent Test Variation");
                    LogMessage("Test never reported any variations.");
                    LogResult(Result.Fail);
                    EndVariation("Non-existent Test Variation");
                }
                hasConnection = false;
                currentVariationName = null;
                Loggers.EndTest(testName);
            }
            currentTestIndex++;
            currentVariationIndex = -1;
            //Ignore if test was not started. Cleanup will catch this.
        }

        public void EndVariation(string variationName)
        {
            if (LoggingState == LoggingState.IsConnected) //We're in a semi-bogus state. Synthesize sequence of corrective actions.
            {
                BeginVariation("Missing BeginVariation");
                LogMessage("BUG: No BeginVariation message was recieved");
                LogResult(Result.Fail);
                EndVariation("Missing BeginVariation");
            }
            else if (LoggingState == LoggingState.HasVariation) //Proper scenario
            {
                Loggers.EndVariation(variationName);
                currentVariationName = null;
                currentResult = null;
                currentTestLogResult = null;
            }
        }

        // This method includes policy for copying a logged file over to the
        // test log directory for archival.
        public void LogFile(string fileName)
        {
            FileInfo fileInfo = new FileInfo(fileName);
            if (!fileInfo.Exists)
            {
                LogMessage("Test attempted to log the file " + fileInfo.FullName + ", which doesn't exist.");
                return;
            }

            // This logic will need to be revised when we add ExecutionGroups, likely by having
            // another level of hierarchy for test name. For now the algorithm is simple
            // enough I'll leave it here - it may make sense to break it out into a separate
            // method if it becomes notably more complex.

            // Determine the directory into which logged files should be copied.
            // We start with the test log directory were are given for the test,
            // where a logged file will go by default. If there is a current
            // variation, however, we'll log the file to a subfolder named based
            // on the variation index. This allows per-variation logged file
            // isolation. For example, each variation in a test may log some
            // result evidence file with the same name, and we don't want those
            // overwriting each other.
            string computedDirectoryPath = Path.Combine(testLogDirectory.FullName, ("T" + currentTestIndex.ToString()));
            if (LoggingState == LoggingState.HasVariation)
            {
                computedDirectoryPath = Path.Combine(computedDirectoryPath, ("V" + currentVariationIndex.ToString()));
            }

            if (!Directory.Exists(computedDirectoryPath))
            {
                Directory.CreateDirectory(computedDirectoryPath);
            }

            FileInfo newFile = fileInfo.CopyTo(Path.Combine(computedDirectoryPath, fileInfo.Name), true);
            Loggers.LogFile(newFile.FullName);
        }

        public void LogMessage(string message)
        {
            Loggers.LogMessage(message);
        }

        public void LogObject(object payload)
        {
            LogMessage(payload.ToString()); //TODO:Do something better with objects
        }

        public void LogProcess(int processId)
        {
            Loggers.LogProcess(processId);
            debuggingEngine.OnProcessRegister(processId);
        }

        public void LogProcessCrash(int processId)
        {
            Loggers.LogProcessCrash(processId);
            debuggingEngine.OnProcessCrash(processId);
        }

        public void LogResult(Result result)
        {
            if (LoggingState == LoggingState.HasVariation)
            {
                Loggers.LogResult(result);
                currentResult = result;
            }
            //else, no-op
        }


        public string GetCurrentTestName()
        {
            // In Initialized we know the test name since the infra has registered
            // it with us, but the test has not actually called BeginTest. So for
            // the purpose of an external party querying for the name of the current
            // test we want to return null.
            if (LoggingState == LoggingState.Initialized)
            {
                return null;
            }
            // Client may need to know if we are debugging. The primary example
            // is BeginTest - in normal scenarios if we call BeginTest and there
            // is already a test the client should likely throw an exception, but
            // in the case of rerun for debugging, we want to allow this. Adding
            // '_QVDEBUG' to the end of the test name is a handshake between
            // here and LogManager.
            else if (isDebugging)
            {
                return currentTestName + "_QVDEBUG";
            }
            else
            {
                return currentTestName;
            }
        }

        public string GetCurrentVariationName()
        {
            return currentVariationName;
        }

        public Result? GetCurrentVariationResult()
        {
            return currentResult;
        }

        public int? GetCurrentTestLogResult()
        {
            return currentTestLogResult;
        }

        public void SetCurrentTestLogResult(int result)
        {
            currentTestLogResult = result;
        }

        public void Reset()
        {
            currentTestIndex = 0;
            currentVariationName = null;
            currentVariationIndex = -1;
            hasReceivedBeginTest = false;
            hasReceivedBeginVariation = false;
        }

        #endregion

        #region Private Members

        private string currentTestName
        {
            get
            {
                if (registeredTests == null) return null;
                if (currentTestIndex < 0 || currentTestIndex >= registeredTests.Count) return null;
                return registeredTests[currentTestIndex].TestInfo.Name;
            }
        }

        private LoggingState LoggingState
        {
            get
            {
                if (!IsListening) return LoggingState.Offline;
                else if (IsListening && !hasConnection) return LoggingState.Initialized;
                else if (IsListening && hasConnection && currentVariationName == null) return LoggingState.IsConnected;
                else return LoggingState.HasVariation;
            }
        }

        #endregion
    }

    internal enum LoggingState
    {
        // Test has been registered by the infra
        Initialized,
        // Test has begun
        IsConnected,
        // Test has an active variation
        HasVariation,
        // Initial state.
        Offline
    }
}
