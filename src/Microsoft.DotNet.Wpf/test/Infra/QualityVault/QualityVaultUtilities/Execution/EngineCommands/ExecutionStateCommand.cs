// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Microsoft.Test.Execution.StateManagement;

namespace Microsoft.Test.Execution.EngineCommands
{
    /// <summary>
    /// Handles State Management operations on per-test execution basis
    /// </summary>
    internal class ExecutionStateCommand : ICleanableCommand
    {
        private ExecutionStateCommand() { }

        public static ExecutionStateCommand Apply(TestRecord test, DirectoryInfo testBinariesDirectory)
        {
            ExecutionStateCommand command = new ExecutionStateCommand();
            ExecutionEventLog.RecordStatus("Applying Execution State.");
            StateCollection.ApplyDeployments(test.TestInfo.Deployments, testBinariesDirectory);
            return command;
        }

        public void Cleanup()
        {
            ExecutionEventLog.RecordStatus("Cleaning up Execution state.");
            StateManagementEngine.PopAllStatesFromPool(StatePool.Execution);
        }
    }

}