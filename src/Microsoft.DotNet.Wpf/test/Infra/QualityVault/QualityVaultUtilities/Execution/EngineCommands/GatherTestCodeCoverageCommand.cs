// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Microsoft.Test.Reporting;

namespace Microsoft.Test.Execution.EngineCommands
{
    /// <summary>
    /// This Component Collects Code Coverage data for a test, temporarily storing it in the log directory until it gets aggregated to server.
    /// </summary>
    internal class GatherTestCodeCoverageCommand : ICleanableCommand
    {
        private DirectoryInfo logDirectory;
        private TestRecord test;

        internal static GatherTestCodeCoverageCommand Apply(TestRecord test, DirectoryInfo logDirectory)
        {
            return new GatherTestCodeCoverageCommand(test, logDirectory);
        }

        internal GatherTestCodeCoverageCommand(TestRecord test, DirectoryInfo logDirectory)
        {
            this.logDirectory = logDirectory;
            this.test = test;
            ExecutionEventLog.RecordStatus("Beginning Code Coverage session.");
            CodeCoverageUtilities.BeginTrace();
        }

        public void Cleanup()
        {
            try
            {
                bool retainResults = (ReportingUtilities.InterpretTestOutcome(test) == Result.Pass || (test.TestInfo.Bugs != null && test.TestInfo.Bugs.Count != 0));
                string traceName = MakeTraceName(test);
                ExecutionEventLog.RecordStatus("Ending Code Coverage session - Saving Code Coverage Trace Results.");
                CodeCoverageUtilities.EndTrace(logDirectory, retainResults, traceName);
            }
            //Hitting a null-ref somewhere in the cleanup logic - need to understand source.
            catch (Exception e)
            {
                ExecutionEventLog.RecordException(e);
            }
        }

        private string MakeTraceName(TestRecord test)
        {
            string area = test.TestInfo.Area;
            string subArea = test.TestInfo.SubArea;
            string priority = test.TestInfo.Priority.ToString();
            string testName = test.TestInfo.Name;
            string separator = "~~";

            string traceName =
                String.Format("{0}{1}{2}{3}{4}{5}{6}",
                string.IsNullOrEmpty(area) ? "(NoArea)" : area,
                separator,
                string.IsNullOrEmpty(subArea) ? "(NoSubArea)" : subArea,
                separator,
                string.IsNullOrEmpty(priority) ? "(NoPriority)" : priority,
                separator,
                string.IsNullOrEmpty(testName) ? "(NoTestName)" : testName);
            return traceName;
        }
    }
}