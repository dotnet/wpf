// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Test.Execution.Logging;

namespace Microsoft.Test.Execution.EngineCommands
{    
    /// <summary>
    /// Performs post-processing of Logs
    /// </summary>
    internal class ProcessLogsCommand : ICleanableCommand
    {
        private IEnumerable<TestRecord> Tests;
        private LoggingMediator mediator;

        private ProcessLogsCommand() { }

        public static ProcessLogsCommand Apply(IEnumerable<TestRecord> tests, LoggingMediator mediator)
        {
            ProcessLogsCommand command = new ProcessLogsCommand();
            command.Tests = tests;
            command.mediator = mediator;
            return command;          
        }

        /// <summary>
        /// Prune out all log text if test is passing. 
        /// If any failure data is present, strip out problematic characters.
        /// </summary>
        public void Cleanup()
        {
            ExecutionEventLog.RecordStatus("Post-processing logs.");

            foreach (TestRecord test in Tests)
            {
                if (test.Variations.All(variation => variation.Result == Result.Pass))
                {
                    test.Log = String.Empty;
                    foreach (VariationRecord variation in test.Variations)
                    {
                        variation.Log = String.Empty;
                    }
                }
                else
                {
                    test.Log = StripInvalidXmlCharacters(test.Log);
                    foreach (VariationRecord variation in test.Variations)
                    {
                        variation.Log = StripInvalidXmlCharacters(variation.Log);
                    }

                    // In the case where a test had failing variations, we want
                    // to also include references to execution group log files
                    // that can provide context for the environment in which
                    // the test was run.
                    foreach (FileInfo file in mediator.ExecutionLogFiles)
                    {
                        test.ExecutionLogFiles.Add(file);
                    }
                }
            }
        }

        private static string StripInvalidXmlCharacters(string s)
        {
            if (String.IsNullOrEmpty(s))
            {
                return s;
            }

            StringBuilder sb = new StringBuilder(s.Length);
            foreach (char c in s)
            {
                if (IsValidXmlCharacter(c))
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        // Can't use XmlConvert.IsXmlChar because that is a 4.0 feature.
        private static bool IsValidXmlCharacter(char current)
        {
            return (current == 0x9) ||
                   (current == 0xA) ||
                   (current == 0xD) ||
                   ((current >= 0x20) && (current <= 0xD7FF)) ||
                   ((current >= 0xE000) && (current <= 0xFFFD));
        }
    }


}


         