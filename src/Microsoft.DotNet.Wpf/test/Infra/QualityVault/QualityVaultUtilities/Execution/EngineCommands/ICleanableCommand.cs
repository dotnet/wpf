// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Test.Execution.EngineCommands
{
    /// <summary>
    /// This provides a high reliabilty contract for commands in Test Execution Engine.
    /// 
    /// It provides following Benefits:
    /// 1- Actions are cleaned up after test execution has occurred.
    /// 2- Actions get cleaned up in reverse order of application.
    /// 3- Cleanup operations get Exception containment - meaning, if they blow up, they don't break everything else, but do get logged.
    /// 
    /// Note: We intentionally don't provide an Apply command. Commands should provide a static initializer which creates the rollback 
    ///         command and applies the operation. Present thinking is that it is better for the initializer to fail, and not 
    ///         provide a useless rollback, than to provide a rollback which is going to be attempted, regardless of success of apply.    
    ///    
    /// </summary>
    internal interface ICleanableCommand
    {
        void Cleanup();
    }
}