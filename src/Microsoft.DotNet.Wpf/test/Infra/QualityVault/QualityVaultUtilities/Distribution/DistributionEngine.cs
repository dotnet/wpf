// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Microsoft.Test.Filtering;
using System.Collections.ObjectModel;

namespace Microsoft.Test.Distribution
{
    /// <summary>
    /// Exposes methods for registering for distribution, performing distribution,
    /// and merging back results from distributed execution. Also exposes a method
    /// for distributed execution to get the distribution subdirectory for the set
    /// of tests distributed to a particular key.
    /// </summary>
    internal static class DistributionEngine
    {
        #region Public Members

        // Public members are specified in the order that they get called in the
        // workflow: RegisterKey -> Distribute -> GetDistributedDirectory -> Merge.
        // The distribution directory specified at each stage should be consistent.

        /// <summary>
        /// Registers a key to have tests distributed to.
        /// </summary>
        /// <param name="distributionKey">Distribution key, such as a machine name.</param>
        /// <param name="distributionDirectory">Directory where distribution is performed from.</param>
        public static void RegisterKey(string distributionKey, DirectoryInfo distributionDirectory)
        {
            // Make sure the top-level distribution directory exists - only the
            // first key being registered will need to create this.
            if (!distributionDirectory.Exists)
            {
                distributionDirectory.Create();
            }

            // Create a subdirectory for distribution key.
            DirectoryInfo subdirectory = ComputeDistributedDirectoryName(distributionDirectory, distributionKey);
            subdirectory.Create();

            MachineRecord record = Configuration.QueryMachine();

            FileInfo machineRecordFileInfo = new FileInfo(Path.Combine(subdirectory.FullName, "MachineRecord.xml"));
            using (XmlTextWriter textWriter = new XmlTextWriter(machineRecordFileInfo.Open(FileMode.Create, FileAccess.Write), System.Text.Encoding.UTF8))
            {
                textWriter.Formatting = Formatting.Indented;
                ObjectSerializer.Serialize(textWriter, record);
            }
        }

        /// <summary>
        /// Distributes tests to a set of distribution keys that were previously
        /// registered via RegisterKey. The distribution directory specified for
        /// RegisterKey and Distribute must match.
        /// </summary>
        /// <param name="records">Tests to distribute.</param>
        /// <param name="distributionDirectory">Directory under which tests will be distributed.</param>
        /// <param name="strategyPrefix">Prefix of strategy to distribute the tests.</param>
        /// <param name="testBinariesDirectory">Location of test binaries.</param>
        public static void Distribute(TestRecords records, DirectoryInfo distributionDirectory, string strategyPrefix, DirectoryInfo testBinariesDirectory)
        {
            // Directory name == distribution key
            DirectoryInfo[] subdirectories = distributionDirectory.GetDirectories();
            MachineRecord[] machines = subdirectories.Select(directory => (MachineRecord)ObjectSerializer.Deserialize(new XmlTextReader(Path.Combine(directory.FullName, "MachineRecord.xml")), typeof(MachineRecord), null)).ToArray();

            int subdirectoriesCount = subdirectories.Length;

            if (subdirectoriesCount == 0)
            {
                throw new ApplicationException("You should execute RegisterForDistribution before DiscoverAndDistribute.");
            }
            if (records.ExecutionGroupRecords.Count > 0)
            {
                throw new ApplicationException("Execution Group Records are already populated - tests appear to already have run.");
            }

            List<TestRecords> partitionedTests = null;

            DistributionStrategy strategy = DistributionStrategy.CreateDistributionStrategy(strategyPrefix);

            partitionedTests = strategy.PartitionTests(records, machines, testBinariesDirectory);

            for (int i = 0; i < subdirectoriesCount; i++)
            {
                // Save each subset of tests to a directory for each registered key.
                partitionedTests[i].Save(subdirectories[i]);
            }
        }

        /// <summary>
        /// Allows distributed execution to obtain the distribution subdirectory
        /// associated to the set of tests distributed to a particular key.
        /// </summary>
        /// <remarks>
        /// Distributed execution uses this obtained directory for loading the
        /// TestCollection, and for where to log files to during execution.
        /// </remarks>
        /// <param name="distributionKey">Key to index into a subset of tests.</param>
        /// <param name="distributionDirectory">Root distribution directory for the full set of tests.</param>
        /// <returns>Directory for distributed subset of tests.</returns>
        public static DirectoryInfo GetDistributedDirectory(string distributionKey, DirectoryInfo distributionDirectory)
        {
            return ComputeDistributedDirectoryName(distributionDirectory, distributionKey);
        }

        /// <summary>
        /// Merges the results of distributed execution back together.
        /// </summary>
        /// <param name="distributionDirectory">Root distribution directory for the full set of tests.</param>        
        /// /// <param name="useCodeCoverage">Specifies if distribution should account for code coverage data</param>
        /// <param name="codeCoverageConnection">Connection string for Code Coverage Database</param>        
        public static void Merge(DirectoryInfo distributionDirectory, bool useCodeCoverage, string codeCoverageConnection)
        {
            DirectoryInfo[] subdirectories = distributionDirectory.GetDirectories();

            TestRecords mergedTestResults = new TestRecords();

            for (int i = 0; i < subdirectories.Length; i++)
            {
                TestRecords deserializedTestResults = TestRecords.Load(subdirectories[i]);
                // If for a testcollection it looks like nothing was run, this
                // suggests badness and we want to halt the merge. If we proceed
                // along with a merge, we end up deleted the TestCollection
                // subsets, making the run unsalvagable. By failing hard here,
                // the culprit set of tests can be rerun, and then merge
                // attempted again.
                if (!ValidateTestsWereExecuted(deserializedTestResults))
                {
                    throw new ApplicationException("TestCollection located in the " + subdirectories[i].FullName + " directory did not have any results. It is highly likely that execution on the associated machine failed. Aborting merge.");
                }

                foreach (TestRecord test in deserializedTestResults.TestCollection)
                {
                    mergedTestResults.TestCollection.Add(test);
                }

                foreach (ExecutionGroupRecord group in deserializedTestResults.ExecutionGroupRecords)
                {
                    mergedTestResults.ExecutionGroupRecords.Add(group);
                }
            }

            mergedTestResults.Save(distributionDirectory);

            if (useCodeCoverage)
            {
                CodeCoverageUtilities.MergeCodeCoverage(subdirectories, distributionDirectory);
                Console.WriteLine("Attempting to Upload Results.");
                CodeCoverageUtilities.UploadCodeCoverage(distributionDirectory, codeCoverageConnection);
                CodeCoverageUtilities.DeleteCodeCoverageMergeInputs(subdirectories);
            }

            // Once we have safely merged the test collections and saved the merged
            // TestCollection to disk, delete those distributed subsets.
            for (int i = 0; i < subdirectories.Length; i++)
            {
                TestRecords.Delete(subdirectories[i]);
            }
        }        

        private static bool ValidateTestsWereExecuted(TestRecords results)
        {
            // If for every TestRecord Execution was not Enabled, then nothing
            // was expected to be run, so we're fine.
            if (results.TestCollection.All(testCollection => !testCollection.ExecutionEnabled))
            {
                return true;
            }

            // If for every TestRecord there were no variations, it looks like
            // nothing was run.
            if (results.TestCollection.All(testCollection => testCollection.Variations.Count == 0))
            {
                return false;
            }

            return true;
        }

        #endregion

        #region Private Members

        private static DirectoryInfo ComputeDistributedDirectoryName(DirectoryInfo directoryPath, string key)
        {
            return new DirectoryInfo(Path.Combine(directoryPath.FullName, key));
        }

        #endregion
    }
}
