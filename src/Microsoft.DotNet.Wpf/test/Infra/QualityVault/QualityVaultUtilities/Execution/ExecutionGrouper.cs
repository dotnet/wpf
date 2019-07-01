// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Test.Execution
{
    /// <summary>
    /// Provides Execution Grouping logic for grouping like tests together for improved performance/reduced overhead.
    /// </summary>  
    internal static class ExecutionGrouper
    {

        /// <summary>
        /// Bucketizes tests into execution groups, and creates separate buckets for tests which do not satisfy the minimum execution grouping level.
        /// </summary>      
        public static IEnumerable<List<TestRecord>> Bucketize(ICollection<TestRecord> tests, ExecutionGroupingLevel minGroupingLevel, Func<TestRecord, object> hashMethod)
        {
            List<TestRecord> groupableTests = new List<TestRecord>(tests.Where(test => Exceeds(test.TestInfo.ExecutionGroupingLevel, minGroupingLevel)));
            List<TestRecord> isolatedTests = new List<TestRecord>(tests.Where(test => !Exceeds(test.TestInfo.ExecutionGroupingLevel, minGroupingLevel)));
            Dictionary<object, List<TestRecord>> bucketizedTests = ExecutionGrouper.HashTests(groupableTests, hashMethod);
            IEnumerable<List<TestRecord>> testGroups = Unify(bucketizedTests, isolatedTests);
            return testGroups;
        }

        /// <summary>
        /// Bucketizes every test into its own group - this is used for Code Coverage scenario to isolate each test into separate appdomain.
        /// </summary>        
        internal static IEnumerable<List<TestRecord>> MakeGroupPerTest(List<TestRecord> uniformTestGroup)
        {
            List<List<TestRecord>> groups = new List<List<TestRecord>>();
            foreach (TestRecord test in uniformTestGroup)
            {
                List<TestRecord> tests = new List<TestRecord>();
                tests.Add(test);
                groups.Add(tests);
            }
            return groups;
        }

        private static bool Exceeds(ExecutionGroupingLevel? executionGroupingLevel, ExecutionGroupingLevel minGroupingLevel)
        {
            return (executionGroupingLevel.HasValue && executionGroupingLevel.Value >= minGroupingLevel);
        }

        /// <summary>
        /// Merges a set of execution groups with a set of tests so we have a uniform enumeration to walk.
        /// </summary>
        private static IEnumerable<List<TestRecord>> Unify(Dictionary<object, List<TestRecord>> executionGroups, List<TestRecord> isolatedTests)
        {
            List<List<TestRecord>> groups = new List<List<TestRecord>>();
            // Current implementation puts the speedy tests first, then goes through the heavily isolated tests. Mainly a concern from recoverability and test order perspective.
            groups.AddRange(executionGroups.Values);
            foreach (TestRecord test in isolatedTests)
            {
                List<TestRecord> tests = new List<TestRecord>();
                tests.Add(test);
                groups.Add(tests);
            }
            return groups;
        }

        private static Dictionary<object, List<TestRecord>> HashTests(ICollection<TestRecord> tests, Func<TestRecord, object> hashMethod)
        {
            Dictionary<object, List<TestRecord>> hashTable = new Dictionary<object, List<TestRecord>>();
            foreach (TestRecord test in tests)
            {                
                TryAdd(test, hashMethod, hashTable);                
            }            
            
            return hashTable;
        }

        private static void TryAdd(TestRecord test, Func<TestRecord, object> hashMethod, Dictionary<object, List<TestRecord>> hashTable)
        {
            List<TestRecord> list = ObtainList(hashMethod(test), hashTable);
            list.Add(test);
        }

        /// <summary>
        /// Get the hashed list of tests, or create a new one if it doesn't yet exist.
        /// </summary>
        private static List<TestRecord> ObtainList(object hash, Dictionary<object, List<TestRecord>> hashTable)
        {
            List<TestRecord> record;
            if (!hashTable.ContainsKey(hash))
            {
                record = new List<TestRecord>();
                hashTable.Add(hash, record);
            }
            else
            {
                record = hashTable[hash];
            }
            return record;
        }
    }
}