// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.IO;
using Microsoft.Test.CommandLineParsing;

namespace Microsoft.Test.Commands
{
    /// <summary>
    /// Generates a complete test run report.
    /// </summary>
    [Description("Generates a complete test run report.")]
    public class MakeReportCommand : Command
    {
        /// <summary>
        /// Points to the directory to save report data to.
        /// </summary>
        [Description("Directory to save report data to.")]
        [Required()]
        public DirectoryInfo ReportDirectory { get; set; }

        /// <summary>
        /// RunDirectory points to the directory where the TestCollection files are stored.
        /// </summary>
        [Description("Centralized directory where run data is stored.")]
        [Required()]
        public DirectoryInfo RunDirectory { get; set; }

        /// <summary>
        /// Points to the directory to copy report XML style sheets from.
        /// </summary>
        [Description("Directory to copy report XSL style sheets from.")]
        [Required()]
        public DirectoryInfo TestBinariesDirectory { get; set; }


        /// <summary>
        /// Encapsulates logic for reporting results for non-interactive analysis.
        /// </summary>
        public override void Execute()
        {
            TestRecords tests = TestRecords.Load(RunDirectory);
            RunInfo runInfo = RunInfo.Load(RunDirectory);
            tests.GenerateXmlReport(tests, runInfo, ReportDirectory, TestBinariesDirectory);
        }
    }
}