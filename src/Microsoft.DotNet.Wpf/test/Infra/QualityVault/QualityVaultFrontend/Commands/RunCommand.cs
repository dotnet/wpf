// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.IO;
using Microsoft.Test.CommandLineParsing;
using Microsoft.Test.Execution;
using Microsoft.Test.Filtering;

namespace Microsoft.Test.Commands
{
    /// <summary/>
    [Description("Runs tests for local execution.")]
    public class RunCommand : FilterableCommand
    {
        /// <summary>
        /// DiscoveryInfo describes a set of DiscoveryTargets to use.
        /// </summary>
        [Description("Path to discovery info xml file.")]
        [Required()]
        public FileInfo DiscoveryInfoPath { get; set; }

        /// <summary>
        /// Whether to re-run failures that have not produced crash dumps for stability.
        /// </summary>
        [Description("Whether to re-run failures")]
        public bool RerunFailures { get; set; }

        /// <summary>
        /// Allows test execution to resume run from last known state, rather than restarting the run from scratch. Note: Any detected differences of the test payload (Discovered test data or filter settings) will cause a resumed run to abort.
        /// </summary>
        [Description("Allows test execution to resume run from last known state, rather than restarting the run from scratch. Note: Any detected differences of the test payload (Discovered test data or filter settings) will cause a resumed run to abort.")]
        public bool ContinueExecution { get; set; }

        /// <summary>
        /// Enables CodeCoverage when set true.
        /// </summary>
        [Description("Enables CodeCoverage when set true")]
        public bool CodeCoverage { get; set; }

        /// <summary>
        /// Specifies database connection string with quotes. Required when doing CC runs.
        /// </summary>
        [Description("Specifies database connection string with quotes. Required when doing CC runs.")]
        public string CodeCoverageImport { get; set; }

        /// <summary>
        /// RunDirectory points to the directory where the TestCollection files are stored.
        /// </summary>
        [Description("Centralized directory where run data is stored.")]
        [Required()]
        public DirectoryInfo RunDirectory { get; set; }

        /// <summary>
        /// Specified to run all tests in a fixed directory instead of the
        /// default behavior of a unique directory per test. This is useful
        /// when doing remote debugging, but can lead to test isolation issues.
        /// </summary>
        [Description("Specified to run all tests in a fixed directory instead of the default behavior of a unique directory per test. This is useful when doing remote debugging, but can lead to test isolation issues.")]
        public DirectoryInfo FixedTestExecutionDirectory { get; set; }

        /// <summary>
        /// Engages Visual Studio Debugging of tests when set true.
        /// </summary>
        [Description("Engages Visual Studio Debugging of tests when set true.")]
        public bool DebugTests { get; set; }

        /// <summary>
        /// Allows per-test debugging if set true. Tests are passed a parameter that signifies they
        /// should wait for a debugger to attach before continuing execution.
        /// </summary>
        [Description("Test waits for debugger to attach.")]
        public bool WaitForDebugger { get; set; }

        /// <summary>
        /// Engages Visual Studio Debugging of tests when set true.
        /// </summary>
        [Description("Sti will wait for debugger to attach.")]
        public bool DebugSti { get; set; }

        /// <summary>
        /// Jit debugger to register.
        /// </summary>
        [Description("Specifies a custom Jit debugger command to register. If unspecified, the default debugger is used.")]
        public string JitDebuggerCommand { get; set; }

        /// <summary>
        /// Specifies a custom multiplier to apply for Test Timeouts. If a negative value is supplied no timeout will occur.
        /// </summary>
        [Description("Specifies a custom multiplier to apply for Test Timeouts. If 0 or a negative value is supplied no timeout will occur.")]
        public float? TimeoutMultiplier { get; set; }

        /// <summary>
        /// Skips DxDiag.
        /// </summary>
        [Description("Does not run DxDiag, when set true.")]
        public bool SkipDxDiag { get; set; }

        /// <summary>
        /// Encapsulates logic for running tests.
        /// </summary>
        public override void Execute()
        {
            if (!ContinueExecution)
            {
                // If the run directory already exists, we need to get rid of it for
                // now. In the future we may add a directory versioning scheme so
                // that the results of multiple runs can be retained.
                if (RunDirectory.Exists)
                {
                    RunDirectory.Delete(true);
                }
                RunDirectory.Create();
            }
            else
            {
                if (!RunDirectory.Exists)
                {
                    throw new InvalidOperationException("No run directory exists - the ContinueExecution mode is only intended for finishing an incomplete run.");
                }
            }

            // If we are in continue execution, can we skip ahead to execute again?
            // More importantly, are we risking badness by not doing so?

            TestRecords.RegisterKey(Environment.MachineName, RunDirectory);

            FilteringSettings filteringSettings = FilteringSettings;
            TestRecords tests = TestRecords.Discover(DiscoveryInfoPath, filteringSettings);
            tests.Filter(filteringSettings, DiscoveryInfoPath.Directory);
            tests.Distribute(RunDirectory, null, DiscoveryInfoPath.Directory);

            DirectoryInfo distributedExecutionDirectory = TestRecords.GetDistributedDirectory(Environment.MachineName, RunDirectory);
            tests = TestRecords.Load(distributedExecutionDirectory);
            ExecutionSettings settings = new ExecutionSettings();
            settings.Tests = tests;
            settings.TestBinariesDirectory = DiscoveryInfoPath.Directory;
            settings.DebugTests = DebugTests;
            settings.DebugSti = DebugSti;
            settings.WaitForDebugger = WaitForDebugger;
            settings.LogFilesPath = distributedExecutionDirectory;
            settings.FixedTestExecutionDirectory = FixedTestExecutionDirectory;
            settings.JitDebuggerCommand = JitDebuggerCommand;
            settings.TimeoutMultiplier = TimeoutMultiplier;
            settings.ContinueExecution = ContinueExecution;
            settings.CodeCoverageEnabled = CodeCoverage;
            settings.CodeCoverageImport = CodeCoverageImport;
            settings.RerunFailures = RerunFailures;
            settings.SkipDxDiag = SkipDxDiag;
            CodeCoverageUtilities.ValidateForCodeCoverage(CodeCoverage, CodeCoverageImport);
            tests.Execute(settings);
            tests.Save(distributedExecutionDirectory);
            ExecutionBackupStore.ClearAllIntermediateTestResults(settings.LogFilesPath);

            TestRecords.Merge(RunDirectory, CodeCoverage, CodeCoverageImport);

            // RunCommand's report policy will be to publish reports to a
            // Report subdirectory of the RunDirectory.
            tests = TestRecords.Load(RunDirectory);
            string reportDirectoryPath = Path.Combine(RunDirectory.FullName, "Report");
            RunInfo runInfo = RunInfo.FromOS();
            runInfo.Save(RunDirectory);
            Console.WriteLine("Saving Report to: {0}\\LabReport.xml", reportDirectoryPath);
            tests.GenerateXmlReport(tests, runInfo, new DirectoryInfo(reportDirectoryPath), DiscoveryInfoPath.Directory);
            tests.DisplayConsoleSummary();
        }
    }
}
