// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Microsoft.Test.Reporting;

namespace Microsoft.Test.Execution.EngineCommands
{   
   /// <summary>
   /// Merges all of the machine centric code coverage data.
   /// </summary>
    internal class MergeCodeCoverageDataCommand : ICleanableCommand
    {
        private DirectoryInfo executionLogPath;

        internal static MergeCodeCoverageDataCommand Apply(DirectoryInfo executionLogPath)
        {
            return new MergeCodeCoverageDataCommand(executionLogPath);
        }

        internal MergeCodeCoverageDataCommand(DirectoryInfo executionLogPath)
        {
            ExecutionEventLog.RecordStatus("Code Coverage Run Enabled.");
            this.executionLogPath=executionLogPath;            
        }

        /// <summary>
        /// Harvest the coverage data, and after we've successfully completed this step, delete the inputs.
        /// A local machine will have a single results\CodeCoverage directory. Queen Bee needs to merge results to then merge to single set in lab scenario.
        /// </summary>
        public void Cleanup()
        {
            ExecutionEventLog.RecordStatus("Locally merging Code Coverage Results.");
            CodeCoverageUtilities.MergeSingleMachineResults(executionLogPath);            
        }
    }
}
