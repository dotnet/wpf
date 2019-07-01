// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.Test.Logging;

namespace Microsoft.Test.Execution.Logging
{
    /// <summary>
    /// Packages Test log data to TestRecord and VariationRecords
    /// Harness side starts each test sesion via RegisterTest, and the remote test closes the session with EndTest.
    /// Recording Logger enforces correct test semantics by recording violations as failures.
    /// </summary>
    internal class RecordingLogger : LoggerBase
    {
        #region Private Data

        private List<TestRecord> registeredTests;
        private int currentTestIndex = -1;
        private VariationRecord currentVariation;
        private StringBuilder logBuilder;
        private StringBuilder variationLogBuilder;

        #endregion

        #region Constructor

        public RecordingLogger()
        {
            logBuilder = new StringBuilder();
            variationLogBuilder = new StringBuilder();
        }

        #endregion

        #region Public Members

        private TestRecord currentTest
        {
            get
            {
                if (registeredTests == null) return null;
                if (currentTestIndex < 0 || currentTestIndex >= registeredTests.Count) return null;
                return registeredTests[currentTestIndex];
            }
        }

        public void RegisterTests(List<TestRecord> testRecords)
        {
            registeredTests = testRecords;
            currentTestIndex = 0;            
        }

        #endregion

        #region Private Members

        public override void BeginTest(string testName)
        {
            logBuilder.Length = 0;

            if (!String.Equals(this.currentTest.TestInfo.Name, testName))
            {
                ExecutionEventLog.RecordStatus(string.Format(CultureInfo.InvariantCulture, "BeginTest specified '{0}' but test name registered was '{1}'.", testName, this.currentTest.TestInfo.Name));
                throw new Exception("This is super serious now that we have execution groups, we cannot recover safely");
            }
        }

        public override void EndTest(string testName)
        {
            currentTest.Log = logBuilder.ToString();
            logBuilder.Length = 0;
            currentTestIndex++;
        }

        public override void BeginVariation(string variationName)
        {
            currentVariation = new VariationRecord();
            currentVariation.StartTime = new InfraTime(DateTime.Now);
            currentVariation.VariationId = currentTest.Variations.Count + 1;
            currentTest.Variations.Add(currentVariation);
            currentVariation.VariationName = variationName;
            variationLogBuilder.Length = 0;
        }

        public override void EndVariation(string variationName)
        {
            currentVariation.Log = variationLogBuilder.ToString();
            variationLogBuilder.Length = 0;
            currentVariation.EndTime = new InfraTime(DateTime.Now);
            currentVariation = null;
        }

        public override void LogFile(string fileName)
        {
            FileInfo fileInfo = new FileInfo(fileName);
            if (currentVariation != null)
            {
                currentVariation.LoggedFiles.Add(fileInfo);
                LogMessage("Logged File: " + fileInfo.Name);
            }
            else if (currentTest != null)
            {
                currentTest.LoggedFiles.Add(fileInfo);
                LogMessage("Logged File: " + fileInfo.Name);
            }
        }

        public override void LogObject(object obj)
        {
            if (currentVariation != null)
            {
                variationLogBuilder.AppendLine(obj.ToString()); //TODO:Do something better with objects
            }
            else
            {
                logBuilder.AppendLine(obj.ToString());
            }
        }

        public override void LogMessage(string message)
        {
            if (currentVariation != null)
            {
                variationLogBuilder.AppendLine(message);
            }
            else
            {
                logBuilder.AppendLine(message);
            }
        }

        public override void LogResult(Result result)
        {
            if (currentVariation != null)
            {
                currentVariation.Result = result;
                LogMessage("Logged Result: " + result.ToString());
            }
            //else, no-op
        }

        public override void LogProcess(int processId) { }

        public override void LogProcessCrash(int processId) { }

        public override void Reset()
        {
            foreach (TestRecord test in registeredTests)
            {
                test.Log = null;
                test.Variations.Clear();
            }
            currentTestIndex = 0;
        }

        #endregion
    }
}
