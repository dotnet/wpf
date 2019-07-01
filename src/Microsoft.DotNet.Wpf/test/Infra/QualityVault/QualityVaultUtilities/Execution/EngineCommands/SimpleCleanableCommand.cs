// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Test.Execution.EngineCommands
{
    /// <summary>
    /// This class provides a base implementation for commands which don't CURRENTLY require cleanup.
    /// 
    /// The benefit here is consistency of the code style in the execution engine.
    /// There is an explicit and intentional tradeoff of simplicity in EE in exchange for more complicated code on the command side.
    /// </summary>
    internal abstract class SimpleCleanableCommand : ICleanableCommand
    {
        public void Cleanup() { }
    }
}