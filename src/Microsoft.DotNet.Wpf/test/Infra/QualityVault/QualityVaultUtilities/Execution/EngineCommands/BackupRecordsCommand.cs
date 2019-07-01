// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Collections.Generic;

namespace Microsoft.Test.Execution.EngineCommands
{   
    internal class BackupRecordsCommand : ICleanableCommand
    {
        private List<TestRecord> Tests;
        private DirectoryInfo LogDirectory;

        private BackupRecordsCommand(List<TestRecord> tests, DirectoryInfo logDirectory)
        {
            LogDirectory = logDirectory;
            Tests = tests;
        }

        public static BackupRecordsCommand Apply(List<TestRecord> tests, DirectoryInfo logDirectory)
        {
            return new BackupRecordsCommand(tests, logDirectory);
        }

        public void Cleanup()
        {
            ExecutionBackupStore.SaveIntermediateTestRecords(Tests, LogDirectory);
        }
    }
}