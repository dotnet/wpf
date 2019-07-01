// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Xml;
using Microsoft.Test.Discovery;
using Microsoft.Test.Distribution;
using Microsoft.Test.Execution;
using Microsoft.Test.Filtering;
using Microsoft.Test.Reporting;

namespace Microsoft.Test
{
    /// <summary>
    ///  This type is used for performing aggregate testing operations, providing a payload of a TestRecord collection along with supporting
    ///  Execution Group Records.
    /// </summary>
    public class TestRecords
    {
        #region Public Data

        /// <summary>
        /// Contains Execution Group Data - this describes the structure and cost of the execution hierarchy's overhead.
        /// </summary>
        public Collection<ExecutionGroupRecord> ExecutionGroupRecords { get; set; }

        /// <summary>
        /// Stores Test results Data
        /// </summary>
        public TestCollection TestCollection { get; set; }

        #endregion

        #region Private Data

        private static readonly string serializationFileName = "TestCollection.xml";

        #endregion

        #region Constructors

        /// <summary>
        /// A public Constructor is needed for Deserialization. Not needed for use by consumers.
        /// </summary>
        public TestRecords()
        {
            ExecutionGroupRecords = new Collection<ExecutionGroupRecord>();
            TestCollection = new TestCollection();
        }

        /// <summary/>
        internal TestRecords(IEnumerable<TestRecord> tests)
        {
            ExecutionGroupRecords = new Collection<ExecutionGroupRecord>();
            TestCollection = new TestCollection();

            foreach (TestRecord t in tests)
            {
                TestCollection.Add(t);
            }
        }

        #endregion

        #region Public Static Methods

        /// <summary>
        /// Discover a collection of test cases.
        /// </summary>
        /// <param name="discoveryInfoPath">Path to Discovery Info. If path is relative, it will interpreted as relative to the current directory.</param>
        /// <param name="filteringSettings">Filtering settings.</param>
        /// <returns></returns>
        public static TestRecords Discover(FileInfo discoveryInfoPath, FilteringSettings filteringSettings)
        {
            Profiler.StartMethod();

            IEnumerable<TestInfo> testInfos = DiscoveryEngine.Discover(discoveryInfoPath, filteringSettings);
            TestRecords tests = new TestRecords();
            foreach (TestInfo testInfo in testInfos)
            {
                TestRecord testRecord = new TestRecord();
                testRecord.TestInfo = testInfo;
                tests.TestCollection.Add(testRecord);
            }
            Profiler.EndMethod();
            return tests;
        }

        /// <summary>
        /// Load a previously stored text collection from disk.
        /// </summary>
        /// <param name="serializationDirectory"></param>
        /// <returns></returns>
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public static TestRecords Load(DirectoryInfo serializationDirectory)
        {
            FileInfo resultsFileInfo = new FileInfo(Path.Combine(serializationDirectory.FullName, serializationFileName));
            Profiler.StartMethod();
            TestRecords records = null;
            try
            {
                using (XmlTextReader textReader = new XmlTextReader(resultsFileInfo.OpenRead()))
                {
                    records = (TestRecords)ObjectSerializer.Deserialize(textReader, typeof(TestRecords), null);
                }
            }
            catch (Exception e)
            {
                throw new IOException("Failed to Deserialize: " + resultsFileInfo.FullName, e);
            }
            Profiler.EndMethod();
            return records;
        }

        /// <summary>
        /// Merge together a list of test collections.
        /// This is used for parallel test execution.
        /// </summary>
        /// <returns></returns>
        public static void Merge(DirectoryInfo distributionRoot, bool useCodeCoverage, string codeCoverageConnection)
        {
            DistributionEngine.Merge(distributionRoot, useCodeCoverage, codeCoverageConnection);
        }

        // TODO: Take another look at these two methods:
        // They are required for distributed execution, but their connection to TestCollection
        // is somewhat tenuous. It probably makes sense to refactor somehow, leaving as-is for now.

        /// <summary>
        /// Registers a key to have tests distributed to.
        /// </summary>
        /// <param name="distributionKey">Distribution key, such as a machine name.</param>
        /// <param name="distributionDirectory">Directory where distribution is performed from.</param>
        public static void RegisterKey(string distributionKey, DirectoryInfo distributionDirectory)
        {
            DistributionEngine.RegisterKey(distributionKey, distributionDirectory);
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
            return DistributionEngine.GetDistributedDirectory(distributionKey, distributionDirectory);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Filter a collection of test cases. Goes through each TestRecord
        /// and verifies its TestInfo passes the filtering settings. If it
        /// does not, marks the TestRecord as not enabled for execution,
        /// and provides an explanation.
        /// </summary>
        /// <param name="filteringSettings">Filter to evaluate TestRecord against.</param>
        /// <param name="testBinariesDirectory"/>        
        public void Filter(FilteringSettings filteringSettings, DirectoryInfo testBinariesDirectory)
        {
            Profiler.StartMethod();
            FilteringEngine.Filter(filteringSettings, this, testBinariesDirectory);
            Profiler.EndMethod();
        }

        /// <summary>
        /// Execute a test collection
        /// </summary>
        public void Execute(ExecutionSettings settings)
        {
            Profiler.StartMethod();
            //Implicit Policy: Infra uses the location of itself to determine where to invoke Infra processes                        
            string path = GetType().Assembly.Location;
            settings.InfraBinariesDirectory = new FileInfo(path).Directory;
            settings.Tests = this;

            ExecutionEngine.Execute(settings);
            Profiler.EndMethod();
        }

        /// <summary>
        /// Save a test collection to disk.
        /// </summary>
        /// <param name="serializationDirectory"></param>
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public void Save(DirectoryInfo serializationDirectory)
        {
            Profiler.StartMethod();
            if (!serializationDirectory.Exists)
            {
                serializationDirectory.Create();
            }
            FileInfo resultsFileInfo = new FileInfo(Path.Combine(serializationDirectory.FullName, serializationFileName));
            using (XmlTextWriter textWriter = new XmlTextWriter(resultsFileInfo.Open(FileMode.Create, FileAccess.Write), System.Text.Encoding.UTF8))
            {
                textWriter.Formatting = Formatting.Indented;
                ObjectSerializer.Serialize(textWriter, this);
            }
            Profiler.EndMethod();
        }

        /// <summary>
        /// Distribute a test collection into a set of test collections.
        /// This is used for parallel test execution.
        /// </summary>
        /// <returns></returns>
        public void Distribute(DirectoryInfo distributionRoot, string strategy, DirectoryInfo testBinariesDirectory)
        {
            DistributionEngine.Distribute(this, distributionRoot, strategy, testBinariesDirectory);
        }

        /// <summary>
        /// Display a simple console summary of the aggregate test results
        /// </summary>        
        public void DisplayConsoleSummary()
        {
            Profiler.StartMethod();
            ReportingEngine.WriteSummaryToConsole(this);
            Profiler.EndMethod();
        }

        /// <summary>
        /// Produce a comprehensive static xml report from the test results
        /// </summary>
        public void GenerateXmlReport(TestRecords tests, RunInfo runInfo, DirectoryInfo reportRoot, DirectoryInfo testBinariesDirectory)
        {
            Profiler.StartMethod();
            ReportingEngine.GenerateXmlReport(tests, runInfo, reportRoot, testBinariesDirectory);
            Profiler.EndMethod();
        }

        #endregion

        #region Internal Members

        /// <summary>
        /// Delete the serialized TestCollection stored in the specified serialization directory.
        /// </summary>
        internal static void Delete(DirectoryInfo serializationDirectory)
        {
            FileInfo testCollectionFileInfo = new FileInfo(Path.Combine(serializationDirectory.FullName, serializationFileName));
            testCollectionFileInfo.Delete();
        }

        #endregion

    }
}