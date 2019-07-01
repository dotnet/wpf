// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace Microsoft.Test.Execution.EngineCommands
{
    /// <summary>
    /// Produces a log directory which remains after run.
    /// </summary>
    internal class LogDirectoryCommand : SimpleCleanableCommand
    {
        private LogDirectoryCommand() { }

        public static LogDirectoryCommand Apply(DirectoryInfo directoryInfo, bool skipDxDiag)
        {
            ExecutionEventLog.RecordStatus("Creating Log Directory: " + directoryInfo.FullName);
            if (!directoryInfo.Exists)
            {
                directoryInfo.Create();
            }

            //Produce the DxDiag File
            if (!skipDxDiag)
            {
                MakeDiagnosticRecord(directoryInfo);
            }

            return new LogDirectoryCommand();
        }
        
        private static void MakeDiagnosticRecord(DirectoryInfo directory)
        {
            ProcessUtilities.Run("dxdiag", "/whql:off /t " + Path.Combine(directory.FullName, "HardwareDiagnostic.txt"));
        }
    }
}
