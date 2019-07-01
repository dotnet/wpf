// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.IO;
using Microsoft.Test.CommandLineParsing;
using Microsoft.Test.Execution;

namespace Microsoft.Test.Commands
{
    /// <summary/>
    [Description("Executes a distributed set of tests.")]
    public class ExecuteCommand : Command
    {
        /// <summary>
        /// Whether to re-run failures that have not produced crash dumps for stability.
        /// </summary>
        [Description("Whether to re-run failures (will not occur if crash dumps present)")]
        public bool RerunFailures { get; set; }

        /// <summary>
        /// RunDirectory points to the directory where the TestCollection files are stored.
        /// </summary>
        [Description("Centralized directory where run data is stored.")]
        [Required()]
        public DirectoryInfo RunDirectory { get; set; }

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
        /// A key which is used to index into the appropriate distributed subset of tests.
        /// </summary>
        [Description("Key to index into a set of tests.")]
        [Required()]
        public string DistributionKey { get; set; }

        /// <summary>
        /// Test waits for debugger to attach.
        /// </summary>
        [Description("Engages visual studio debugging of tests")]
        public bool DebugTests { get; set; }
  
        /// <summary>
        /// Allows per-test debugging if set true. Tests are passed a parameter that signifies they
        /// should wait for a debugger to attach before continuing execution.
        /// </summary>
        [Description("Test waits for debugger to attach.")]
        public bool WaitForDebugger { get; set; }

        /// <summary>
        /// Sti waits for debugger to attach
        /// </summary>
        [Description("Sti waits for debugger to attach.")]

        public bool DebugSti { get; set; }

        /// <summary>
        /// Allows test execution to resume run from last known state, rather than restarting the run from scratch. Note: Any detected differences of the specified test payload will cause a resumed run to abort.
        /// </summary>
        [Description("Allows test execution to resume run from last known state, rather than restarting the run from scratch. Note: Any detected differences of the specified test payload will cause a resumed run to abort.")]
        public bool ContinueExecution { get; set; }

        /// <summary>
        /// Points to the directory to copy test binaries from.
        /// </summary>
        [Description("Directory to copy test binaries from.")]
        [Required()]
        public DirectoryInfo TestBinariesDirectory { get; set; }

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
        /// Encapsulates logic for executing.
        /// </summary>
        public override void Execute()
        {
            DirectoryInfo distributedExecutionDirectory = TestRecords.GetDistributedDirectory(DistributionKey, RunDirectory);
            TestRecords tests = TestRecords.Load(distributedExecutionDirectory);

            ExecutionSettings settings = new ExecutionSettings();
            settings.Tests = tests;
            settings.TestBinariesDirectory = TestBinariesDirectory;
            settings.DebugTests = DebugTests;
            settings.DebugSti = DebugSti;
            settings.WaitForDebugger = WaitForDebugger;
            settings.LogFilesPath = distributedExecutionDirectory;
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
            tests.DisplayConsoleSummary();
        }
    }
}
