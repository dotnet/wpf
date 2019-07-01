// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.Test.Distribution
{
    /// <summary>
    /// StressDistributionStrategy: Define the strategy to distribute Stress tests. 
    /// </summary>
    public class StressDistributionStrategy : DistributionStrategy
    {
        /// <summary>
        /// Distribute Stress tests. Each subdivision will get a randomly selected stress test. 
        /// The algorithm ensures that every test will be distributed at least once if the number 
        /// of subdivision is larger than that of available tests, and that every subdivision has 
        /// one and only one test. 
        /// </summary>
        /// <param name="tests">Tests to distribute</param>
        /// <param name="machines">Machines to distribute to</param>
        /// <param name="testBinariesDirectory">Location of test binaries</param>
        /// <returns>List of TestCollection partitioned</returns>
        public override List<TestRecords> PartitionTests(TestRecords tests, MachineRecord[] machines, DirectoryInfo testBinariesDirectory)
        {
            Profiler.StartMethod();

            TestRecords testsToDistribute = SelectEnabledTests(tests);

            int subdivisionCount = machines.Length;

            Random r = new Random();
            List<TestRecords> collections = new List<TestRecords>();
            TestRecords subCollection = null;
            for (int i = 0; i < subdivisionCount; i++)
            {
                subCollection = new TestRecords();

                // Randomly distribute a test only there is any. If no test is available, just return empty list. 
                if (tests.TestCollection.Count != 0)
                {
                    // Get the full collection back if all tests has been distributed out. 
                    if (testsToDistribute.TestCollection.Count == 0)
                    {
                        testsToDistribute = SelectEnabledTests(tests);
                    }

                    // Randomly select a test. 
                    int index = r.Next(testsToDistribute.TestCollection.Count);
                    TestRecord test = testsToDistribute.TestCollection[index];

                    testsToDistribute.TestCollection.Remove(test);

                    subCollection.TestCollection.Add(test);
                }
                collections.Add(subCollection);
            }

            Profiler.EndMethod();

            return collections;
        }

        /// <summary>
        /// Get all Tests whose ExecutionEnabled Property is true, from a TestCollection. 
        /// </summary>
        /// <param name="tests"></param>
        /// <returns></returns>
        private TestRecords SelectEnabledTests(TestRecords tests)
        {
            return new TestRecords(tests.TestCollection.Where(test => ((TestRecord)test).ExecutionEnabled));
        }
    }
}
