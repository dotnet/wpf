// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Test.Logging
{
    /// <summary>
    /// Displays a progress window for test execution.
    /// </summary>
    /// <remarks>
    /// Uses an in-proc Show/Refresh approach that leads to very simple code,
    /// but does mean the UI is non-interactive and can have client area
    /// invalidation artifacts until next refresh. For our scenario this isn't
    /// an issue, but if future requirements change we'd need to look into
    /// hosting the progress window in a separate thread.
    /// </remarks>
    internal class ProgressWindowLogger : LoggerBase
    {
        private ProgressWindow progressWindow;

        /// <summary />
        public ProgressWindowLogger(int testCount)
        {
            progressWindow = new ProgressWindow("Test Case Execution", testCount, "tests executed");
            progressWindow.Show();
            progressWindow.MoveToLowerRight();
            progressWindow.Refresh();
        }

        #region ILogger Members

        /// <summary/>        
        public override void BeginTest(string name)
        {
            // no-op
        }

        /// <summary/>
        public override void EndTest(string name)
        {
            progressWindow.ReportProgress();
            progressWindow.Refresh();
        }

        /// <summary/>
        public override void BeginVariation(string name)
        {
            // no-op
        }

        /// <summary/>
        public override void EndVariation(string name)
        {
            // no-op
        }

        /// <summary/>
        public override void LogFile(string filename)
        {
            // no-op
        }

        /// <summary/>
        public override void LogMessage(string message)
        {
            // no-op
        }

        /// <summary/>
        public override void LogObject(object payload)
        {
            // no-op
        }

        /// <summary/>
        public override void LogResult(Result result)
        {
            // no-op
        }

        /// <summary/>
        public override void LogProcessCrash(int processId)
        {
            // no-op
        }

        /// <summary/>
        public override void LogProcess(int ProcessId)
        {
            // no-op
        }

        #endregion

        internal void Close()
        {
            progressWindow.Close();
        }
    }
}
