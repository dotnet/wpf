// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.IO;
using Microsoft.Test.CommandLineParsing;

namespace Microsoft.Test.Commands
{
    /// <summary/>
    [Description("Registers a key for Distribution")]
    public class RegisterForDistributionCommand : Command
    {
        /// <summary>
        /// Registers a key for distribution.
        /// </summary>
        [Description("Register a key that tests will be distributed to.")]
        [Required()]
        public string DistributionKey { get; set; }

        /// <summary>
        /// RunDirectory points to the directory where the TestCollection files are stored.
        /// </summary>
        [Description("Centralized directory where run data is stored.")]
        [Required()]
        public DirectoryInfo RunDirectory { get; set; }

        /// <summary>
        /// Encapsulates logic for registering a key.
        /// </summary>
        public override void Execute()
        {
            TestRecords.RegisterKey(DistributionKey, RunDirectory);
        }
    }
}
