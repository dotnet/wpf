// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Xml;
using Microsoft.Test;
using System.IO;
using System.Linq;

namespace Microsoft.Test.Reporting
{
    /// <summary>
    /// Reporting specific utilities which have affinity to no specific report generator
    /// </summary>
    internal static class ReportingHelpers
    {        
        internal static Dictionary<string, TestCollection> GroupByArea(TestCollection tests)
        {
            Dictionary<string, TestCollection> buckets = new Dictionary<string, TestCollection>();
            foreach (TestRecord test in tests)
            {
                string area = test.TestInfo.Area;
                TestCollection areaCollection;
                if (!buckets.ContainsKey(area))
                {
                    areaCollection = new TestCollection();
                    buckets.Add(area, areaCollection);
                }
                else
                {
                    areaCollection = buckets[area];
                }
                areaCollection.Add(test);
            }
            return buckets;
        }

        internal static TestCollection FilterForDrts(TestCollection tests)
        {
            return new TestCollection(tests.Where(test => test.TestInfo.Type == TestType.DRT));
        }

        /// <summary>
        /// If you've got to create a directory, but it might already have content, you should scorchify it.
        /// </summary>
        /// <param name="directory"></param>
        internal static void CreateScorchedDir(DirectoryInfo directory)
        {
            if (directory.Exists)
            {
                directory.Delete(true);
            }
            directory.Create();
            //TODO - Eat & log exceptions?
        }


        //Returns true iff at least one variation ran AND they all passed.
        internal static bool TestDefinitelyPassed(TestRecord test)
        {
            bool result = (test.Variations.Count > 0);

            foreach (VariationRecord variation in test.Variations)
            {
                result &= variation.Result == Result.Pass;
            }
            return result;
        }

    }
}