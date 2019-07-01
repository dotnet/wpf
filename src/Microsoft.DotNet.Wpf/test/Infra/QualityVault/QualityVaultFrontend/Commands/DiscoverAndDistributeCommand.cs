// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.IO;
using Microsoft.Test.CommandLineParsing;
using Microsoft.Test.Distribution;
using Microsoft.Test.Filtering;

namespace Microsoft.Test.Commands
{
    /// <summary/>
    [Description("Discovers and Distributes tests for parallel execution.")]
    public class DiscoverAndDistributeCommand : FilterableCommand
    {
        /// <summary>
        /// DiscoveryInfo contains a set of DiscoveryTargets to use.
        /// </summary>
        [Description("Path to discovery info xml file.")]
        [Required()]
        public FileInfo DiscoveryInfoPath { get; set; }

        /// <summary>
        /// RunDirectory points to the directory where the TestCollection files are stored.
        /// </summary>
        [Description("Centralized directory where run data is stored.")]
        [Required()]
        public DirectoryInfo RunDirectory { get; set; }


        /// <summary>
        /// DistributionStrategy : Define how to distribute the tests.
        /// </summary>
        [Description("Prefix for DistributionStrategy to partition tests.")]
        public string DistributionStrategy { get; set; }

        /// <summary>
        /// Encapsulates logic for discovering and distributing tests.
        /// </summary>
        public override void Execute()
        {
            // Area is inherited from FilterableCommand - it is used for both
            // target filtering and filtering of discovered tests.
            FilteringSettings filteringSettings = FilteringSettings;
            TestRecords allTests = TestRecords.Discover(DiscoveryInfoPath, filteringSettings);
            allTests.Filter(filteringSettings, DiscoveryInfoPath.Directory);
            allTests.Distribute(RunDirectory, DistributionStrategy, DiscoveryInfoPath.Directory);
        }
    }
}
