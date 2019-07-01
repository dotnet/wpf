// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Microsoft.Test.Execution.StateManagement;

namespace Microsoft.Test.Execution.EngineCommands
{
    /// <summary>
    /// Provides State Management operation and for entire Test Run.
    /// </summary>
    internal class RunStateCommand : ICleanableCommand
    {
        private RunStateCommand() { }

        public static RunStateCommand Apply(DirectoryInfo testBinariesDirectory)
        {
            ExecutionEventLog.RecordStatus("STARTED  : ------------- Gac Test Infra Assemblies ------------- |");
            StateCollection infraLibraries=StateCollection.LoadStateCollection(@"Infra\GacTestLibraries.deployment", testBinariesDirectory);
            infraLibraries.Push(StatePool.Run);
            ExecutionEventLog.RecordStatus("COMPLETED: ------------- Gac Test Infra Assemblies ------------- |");
            return new RunStateCommand();
        }
    
        public void Cleanup()
        {
            ExecutionEventLog.RecordStatus("STARTED  : ------------ UnGac Test Infra Assemblies ------------ |");
            StateManagementEngine.PopAllStatesFromPool(StatePool.Run);
            ExecutionEventLog.RecordStatus("COMPLETED: ------------ UnGac Test Infra Assemblies ------------ |");
        }
    }
}