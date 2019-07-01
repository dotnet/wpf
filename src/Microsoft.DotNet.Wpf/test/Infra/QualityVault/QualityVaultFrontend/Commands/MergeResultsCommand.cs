// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.IO;
using Microsoft.Test.CommandLineParsing;
using System;

namespace Microsoft.Test.Commands
{
    /// <summary/>
    [Description("Merges results from a set of parallel test executions.")]
    public class MergeResultsCommand : Command
    {
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
        /// Encapsulates logic of merging results.
        /// </summary>
        public override void Execute()
        {
            CodeCoverageUtilities.ValidateForCodeCoverage(CodeCoverage, CodeCoverageImport);
            TestRecords.Merge(RunDirectory, CodeCoverage, CodeCoverageImport);

            //Record the lab Run information at this stage. We assume homogeneous configuration.
            RunInfo.FromEnvironment().Save(RunDirectory);
        }
    }
}