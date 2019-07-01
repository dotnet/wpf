// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.IO;
using Microsoft.Test.Execution.StateManagement;
using System.Collections.Generic;

namespace Microsoft.Test
{
    /// <summary>
    /// Settings class for propagating the many run settings needed by execution engine, without exposing the engine publicly outside of QVU.
    ///
    /// This scales better than explicit method params, while being a little more controlled than global variables.
    ///
    /// This component also contains settings interpretation policy, so the engine itself can be more free of that data.
    /// </summary>
    public class ExecutionSettings
    {
        #region Essential Settings

        /// <summary>
        /// The tests to be run
        /// </summary>
        public TestRecords Tests { get; set; }

        /// <summary>
        /// Read-Only Path of Infrastructure Binaries, used for executing infra and tests. This must be a local path.
        /// </summary>
        public DirectoryInfo InfraBinariesDirectory { get; set; }

        /// <summary>
        /// Read-Only location of Test binaries, provides binaries. We don't run from this, to avoid network issues.
        /// </summary>
        public DirectoryInfo TestBinariesDirectory { get; set; }

        /// <summary>
        /// Write Path for storage of execution logs and results.
        /// </summary>
        public DirectoryInfo LogFilesPath { get; set; }

        #endregion

        #region Optional Settings

        /// <summary>
        /// Enables a single re-run for tests that fail without a .dmp file being created.
        /// </summary>
        public bool RerunFailures { get; set; }

        /// <summary>
        /// If not null, a specific directory in which to execute all tests. Useful for remote debugging, but decreases test isolation.
        /// </summary>
        public DirectoryInfo FixedTestExecutionDirectory { get; set; }

        /// <summary>
        /// Allows per-test debugging if set true.
        /// </summary>
        public bool DebugTests { get; set; }

        /// <summary>
        /// Allows per-test debugging if set true. Tests are passed a parameter that signifies they
        /// should wait for a debugger to attach before continuing execution.
        /// </summary>
        public bool WaitForDebugger { get; set; }

        /// <summary>
        /// Allows debugging of Sti
        /// </summary>
        public bool DebugSti { get; set; }

        /// <summary>
        /// Allows continuation of previously executed tests.
        /// </summary>
        public bool ContinueExecution { get; set; }

        /// <summary>
        /// Enables CodeCoverage when set true.
        /// </summary>
        public bool CodeCoverageEnabled { get; set; }

        /// <summary>
        /// Specifies database connection string with quotes. Required when doing CC runs.
        /// </summary>
        public string CodeCoverageImport { get; set; }

        /// <summary>
        /// Optional string specifying custom Jit Debugger command to employ
        /// </summary>
        public string JitDebuggerCommand { get; set; }

        /// <summary>
        /// Optional Timeout Multiplier.
        ///     Values Greater than 0 are multiplied by test timeouts.
        ///     Values less than or equal to 0 are used as sentinel to cause timeout to be of infinite duration.
        ///     Null value is ignored
        /// </summary>
        public float? TimeoutMultiplier { get; set; }

        /// <summary>
        /// Skips DxDiag.
        /// </summary>
        public bool SkipDxDiag { get; set; }

        #endregion

        #region Derived Settings

        /// <summary>
        /// Root directory for executiong tests.
        /// </summary>
        internal DirectoryInfo ExecutionRootDirectory
        {
            get
            {
                string executionRootPath = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), @"QualityVault\Execution");
                return new DirectoryInfo(executionRootPath);
            }
        }

        /// <summary>
        /// Determines Directory to Log to for the current execution stage
        /// </summary>
        internal DirectoryInfo DetermineTestLogDirectory(string executionLabel)
        {
            return new DirectoryInfo(Path.Combine(LogFilesPath.FullName, executionLabel));
        }

        /// <summary>
        /// Determines the Directory to execute from for the current execution stage
        /// </summary>
        internal DirectoryInfo DetermineTestExecutionDirectory(string executionLabel)
        {
            if (FixedTestExecutionDirectory != null)
            {
                return FixedTestExecutionDirectory;
            }
            else
            {
                return new DirectoryInfo(Path.Combine(ExecutionRootDirectory.FullName, executionLabel));
            }
        }

        /// <summary>
        /// Determine the Execution Group Path from supplied arguments.
        /// </summary>
        internal string DetermineGroupPath(int stateGroupIndex)
        {
            return "S" + stateGroupIndex.ToString();
        }

        /// <summary>
        /// Determine the Execution Group Path from supplied arguments.
        /// </summary>
        internal string DetermineGroupPath(int stateGroupIndex, int supportFileIndex)
        {
            return Path.Combine("S" + stateGroupIndex.ToString(), "F" + supportFileIndex.ToString());
        }

        /// <summary>
        /// Determine the Execution Group Path from supplied arguments.
        /// </summary>
        internal string DetermineGroupPath(int stateGroupIndex, int supportFileIndex, int testIndex)
        {
            return Path.Combine(DetermineGroupPath(stateGroupIndex,supportFileIndex), "T" + testIndex);
        }


        /// <summary>
        /// Provides a default timeout for unspecified cases,
        /// adjusts timeouts in special scenarios.
        /// </summary>
        internal TimeSpan DetermineTimeout(TimeSpan? testSpecifiedTimeout, TestType testType)
        {
            TimeSpan timeout;
            if (testSpecifiedTimeout != null)
            {
                timeout = (TimeSpan)testSpecifiedTimeout;
            }
            else //Start with a default of 90s.
            {
                timeout = TimeSpan.FromSeconds(90);
            }

            // Apply timeout multiplier if present
            if (TimeoutMultiplier != null)
            {
                if (TimeoutMultiplier > 0)
                {
                    timeout = TimeSpan.FromSeconds((timeout.TotalSeconds) * (float)TimeoutMultiplier);
                }
                else
                {
                    timeout = TimeSpan.MaxValue;
                }
            }

            //Debugged tests and stress tests never get timed out.
            if (DebugTests || testType == TestType.Stress || DebugSti)
            {
                timeout = TimeSpan.MaxValue;
            }

            return timeout;
        }

        #endregion
    }

}
