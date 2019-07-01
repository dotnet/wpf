// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Text;
using Microsoft.Test.Execution.Logging;

namespace Microsoft.Test.Execution.EngineCommands
{
    /// <summary>
    /// Creates Execution group directory and stores a log for recording group related events. 
    /// </summary>
    internal class ExecutionGroupLogCommand : ICleanableCommand
    {
        private string groupName;
        private LoggingMediator mediator;
        private DirectoryInfo groupPath;
        private StringBuilder log;

        private ExecutionGroupLogCommand(string groupName, DirectoryInfo groupPath, LoggingMediator mediator)
        {
            this.mediator = mediator;
            this.groupPath = groupPath;
            this.groupName = groupName;
            groupPath.Create();
            log = new StringBuilder();
        }

        public static ExecutionGroupLogCommand Apply(string groupName, DirectoryInfo groupPath, LoggingMediator mediator)
        {
            ExecutionGroupLogCommand command = new ExecutionGroupLogCommand(groupName, groupPath, mediator);
            mediator.PushListener(command);
            return command;
        }

        internal void LogEvent(string message)
        {
            log.AppendLine(message);
        }

        internal void LogFile(string filename)
        {
            log.AppendLine(filename);
        }

        internal FileInfo LogFileLocation
        {
            get
            {
                return new FileInfo(Path.Combine(groupPath.FullName, groupName + "Log.txt"));
            }
        }

        #region ICleanableCommand Members

        public void Cleanup()
        {
            mediator.PopListener();
            using (StreamWriter writer = File.CreateText(LogFileLocation.FullName))
            {
                writer.Write(log.ToString());
            }
        }

        #endregion
    }
}