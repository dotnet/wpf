// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using Microsoft.Test.CommandLineParsing;
using Microsoft.Test.Execution.EngineCommands;

namespace Microsoft.Test.Commands
{
    /// <summary/>
    [Description("Cleans up stateful aspects of Test Infrastructure for local workflow.")]
    public class CleanupCommand : Command
    {
        /// <summary>
        /// Encapsulates logic for cleaning up any infrastructure state.
        /// </summary>
        public override void Execute()
        {
            Console.WriteLine("Cleaning up.");
            DebuggingEngineCommand.Rollback();

            //HACK - We need to know the location of the ES.exe so we can shut it down. This is likely to fail when run from a share.
            string testBinRoot = @".\";
            ElevationServiceCommand.RemoveInstallation(testBinRoot);
        }
    }
}
