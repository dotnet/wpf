// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Test.Filtering;

namespace Microsoft.Test.Distribution
{
    /// <summary>
    /// FunctionalDistributionStrategy: Define the strategy to distribute functional tests. 
    /// </summary>
    public class FunctionalDistributionStrategy : DistributionStrategy
    {
        /// <summary>
        /// Partition functional tests. 
        /// </summary>
        /// <param name="tests">Tests to distribute</param>
        /// <param name="machines">Machines to distribute to</param>
        /// <param name="testBinariesDirectory">Location of test binaries.</param>
        /// <returns>List of TestCollection partitioned</returns>
        public override List<TestRecords> PartitionTests(TestRecords tests, MachineRecord[] machines, DirectoryInfo testBinariesDirectory)
        {
            // We use a round-robin distribution pattern. The algorithm is simple
            // and does a good job of balancing load barring pathological
            // examples, but has the downside of minimizing spatial locality.

            Profiler.StartMethod();

            if (machines.Length <= 0)
            {
                throw new ArgumentException("At least one machine must be specified for distribution.", "machines");
            }

            List<TestRecords> collections = new List<TestRecords>();
            for (int i = 0; i < machines.Length; i++)
            {
                collections.Add(new TestRecords());
            }

            int machineIndex = 0;
            int testIndex = 0;
            int distributionFailCount = 0;

            while (testIndex < tests.TestCollection.Count)
            {
                // If the next test to be distributed can be run on the next machine
                // in our round-robin queue, we'll add it. Additionally, if the
                // distributionFailCount is greater than the number of machines, we've
                // tried to distribute it to each machine, and they have all said that
                // they can't run it. In this case, we'll just say to heck with it and
                // give it to the current machine anyways and continue. Filtering down
                // the line will ensure the test won't actually be run.
                if (MachineCanRunTest(tests.TestCollection[testIndex], machines[machineIndex], testBinariesDirectory) || distributionFailCount > machines.Length)
                {
                    tests.TestCollection[testIndex].Machine = machines[machineIndex];
                    collections[machineIndex].TestCollection.Add(tests.TestCollection[testIndex]);
                    testIndex++;
                    distributionFailCount = 0;
                }
                else
                {
                    distributionFailCount++;
                }

                // Move on to next machine, and wrap around as appropriate.
                machineIndex = (machineIndex + 1) % machines.Length; 
            }

            // Now that we've distributed tests to machines, send them all
            // through configuration filtering so any tests that couldn't be
            // matched to a satisfactory machine get marked to not execute.
            foreach (TestRecords collection in collections)
            {
                foreach (TestRecord test in collection.TestCollection)
                {
                    FilteringEngine.FilterConfigurations(test, testBinariesDirectory);
                }
            }

            Profiler.EndMethod();

            return collections;
        }

        private bool MachineCanRunTest(TestRecord testRecord, MachineRecord machine, DirectoryInfo testBinariesDirectory)
        {
            // Tests with no Configuration Mixes automatically pass this filter.
            if (testRecord.TestInfo.Configurations != null && testRecord.TestInfo.Configurations.Count != 0)
            {
                //Find a configuration mix which the machine satisfies, in order to run the test.
                foreach (string configurationMix in testRecord.TestInfo.Configurations)
                {
                    Configuration mix = Configuration.LoadConfigurationMix(configurationMix, testBinariesDirectory);
                    if (mix.IsSatisfied(machine))
                    {
                        return true;
                    }
                }
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
