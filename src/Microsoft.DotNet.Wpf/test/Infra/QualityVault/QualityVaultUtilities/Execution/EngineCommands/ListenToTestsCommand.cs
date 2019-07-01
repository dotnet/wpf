// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using Microsoft.Test.Execution.Logging;

namespace Microsoft.Test.Execution.EngineCommands
{
    /// <summary>
    /// Signals to logging system to start-end listening
    /// </summary>
    internal class ListenToTestsCommand : ICleanableCommand
    {
        private LoggingMediator Mediator;
        private List<TestRecord> Tests;
        private bool DebugTest;

        private ListenToTestsCommand() { }

        public static ListenToTestsCommand Apply(List<TestRecord> tests, LoggingMediator loggingMediator, DirectoryInfo testLogDirectory, bool debugTest)        
        {
            ListenToTestsCommand command = new ListenToTestsCommand();
            command.Mediator = loggingMediator;
            command.Tests = tests;
            command.DebugTest = debugTest;

            ExecutionEventLog.RecordStatus("Starting Test Logging Transaction.");                        
            command.Mediator.StartTests(tests, testLogDirectory);
            return command;
        }

        public void Cleanup()
        {
            ExecutionEventLog.RecordStatus("Ending TestLog.");
            Mediator.EndTests(Tests, DebugTest);
        }
    }
}
