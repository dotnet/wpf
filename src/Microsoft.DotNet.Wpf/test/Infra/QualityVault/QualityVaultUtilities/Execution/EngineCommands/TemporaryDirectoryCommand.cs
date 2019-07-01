// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace Microsoft.Test.Execution.EngineCommands
{
    /// <summary>
    /// Creates temporary directory which is deleted on revert. Optionally,
    /// overload can preserve directory if it was pre-existing.
    /// </summary>
    internal class TemporaryDirectoryCommand : ICleanableCommand
    {
        private DirectoryInfo DirectoryInfo;
        private bool preserveDirectory;

        private TemporaryDirectoryCommand() { }

        public static TemporaryDirectoryCommand Apply(DirectoryInfo directoryInfo)
        {
            return Apply(directoryInfo, false);
        }

        // If preserveExistingDirectory is true, don't delete on revert if the directory already existed.
        public static TemporaryDirectoryCommand Apply(DirectoryInfo directoryInfo, bool preserveExistingDirectory)
        {
            TemporaryDirectoryCommand directory = new TemporaryDirectoryCommand();
            directory.preserveDirectory = preserveExistingDirectory && directoryInfo.Exists;
            directory.DirectoryInfo = directoryInfo;
            ExecutionEventLog.RecordStatus("STARTED  : ----------- Creating Execution Directory ------------ |"); 
            ExecutionEventLog.RecordStatus("Execution Dir: " + directory.DirectoryInfo.FullName);
            directory.DirectoryInfo.Create();
            ExecutionEventLog.RecordStatus("COMPLETED: ----------- Creating Execution Directory ------------ |"); 

            return directory;
        }

        public void Cleanup()
        {
            if (!preserveDirectory)
            {
                ExecutionEventLog.RecordStatus("STARTED  : ----------- Deleting Execution Directory ------------ |"); 
                DirectoryInfo.Delete(true);
                ExecutionEventLog.RecordStatus("COMPLETED: ----------- Deleting Execution Directory ------------ |"); 

            }
        }
    }
}