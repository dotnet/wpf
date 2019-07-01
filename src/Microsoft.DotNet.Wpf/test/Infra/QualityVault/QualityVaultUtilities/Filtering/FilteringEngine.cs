// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Microsoft.Test.Filtering
{
    /// <summary>
    /// Exposes a static method for filtering a set of TestRecords, marking
    /// them as disabled for execution if they do not pass the filter, and
    /// giving an explanation of why they did not pass.
    /// </summary>
    internal static class FilteringEngine
    {
        /// <summary>
        /// Filter a collection of test cases. Goes through each TestRecord
        /// and verifies its TestInfo passes the filter expression. If it
        /// does not, marks the TestRecord as not enabled for execution,
        /// and provides an explanation. The explanation is inferred by
        /// parsing the expression tree, so the quality of the explanation
        /// can be variable. If a collection of test cases has been filtered
        /// before, the results of that filter are cleared.
        /// </summary>
        /// <param name="filteringSettings">Filter to evaluate TestRecord against.</param>
        /// <param name="testRecords">Set of TestRecords to filter.</param>
        /// <param name="testBinariesDirectory"/>
        public static void Filter(FilteringSettings filteringSettings, TestRecords testRecords, DirectoryInfo testBinariesDirectory)
        {
            filteringSettings.EnsureVersions();

            foreach (TestRecord testRecord in testRecords.TestCollection)
            {
                FilterTestInfo(testRecord, filteringSettings);
            }
        }

        private static void FilterTestInfo(TestRecord testRecord, FilteringSettings filteringSettings)
        {
            testRecord.ExecutionEnabled = false;

            // For now we go through properties alphabetically, but depending
            // upon perf profiling there are potential optimizations. For
            // example, the most frequent part of the filter that will fail is
            // likely Name, in which case we could check that first.

            // Translation of this Linq query:
            // Is there not any Area specified by the filtering settings such that it is a substring of the TestInfo's Area.
            // (.IndexOf >= 0 is used instead of .Contains because we want to be case-insensitive)
            if (filteringSettings.Area != null && !filteringSettings.Area.Any(filterArea => (testRecord.TestInfo.Area ?? String.Empty).IndexOf(filterArea, StringComparison.OrdinalIgnoreCase) >= 0))
            {
                testRecord.FilteringExplanation = String.Format("TestInfo Area value of {0} did not match any of the filter Areas of {1}.", testRecord.TestInfo.Area, CommaSeparatedListExtensions.ToCommaSeparatedList(filteringSettings.Area));
            }
            else if (filteringSettings.Disabled != null && filteringSettings.Disabled != testRecord.TestInfo.Disabled)
            {
                testRecord.FilteringExplanation = String.Format("TestInfo Disabled value of {0} did not match filter Disabled value of {1}.", testRecord.TestInfo.Disabled, filteringSettings.Disabled);
            }
            // For disabled we have special policy - if a disabled filter was not specified, we will still filter out tests marked disabled.
            else if (filteringSettings.Disabled == null && (testRecord.TestInfo.Disabled ?? false))
            {
                testRecord.FilteringExplanation = String.Format("TestInfo was marked disabled and no filter value was specified; excluding test.");
            }
            else if (filteringSettings.Keywords != null && !filteringSettings.Keywords.Any(filterKeyword => (testRecord.TestInfo.Keywords ?? new Collection<string>()).Contains(filterKeyword, StringComparer.OrdinalIgnoreCase)))
            {
                testRecord.FilteringExplanation = String.Format("TestInfo Keywords of {0} did not contain any values matching any of the filter Keywords of {1}.", CommaSeparatedListExtensions.ToCommaSeparatedList(testRecord.TestInfo.Keywords), CommaSeparatedListExtensions.ToCommaSeparatedList(filteringSettings.Keywords));
            }
            else if (filteringSettings.Name != null && !filteringSettings.Name.Any(filterName => (testRecord.TestInfo.Name ?? String.Empty).IndexOf(filterName, StringComparison.OrdinalIgnoreCase) >= 0))
            {
                testRecord.FilteringExplanation = String.Format("TestInfo Name value of {0} did not match filter Name value of {1}.", testRecord.TestInfo.Name, filteringSettings.Name);
            }
            else if (filteringSettings.Priority != null && !filteringSettings.Priority.Any(filterPriority => testRecord.TestInfo.Priority.HasValue ? filterPriority == testRecord.TestInfo.Priority.Value : false))
            {
                testRecord.FilteringExplanation = String.Format("TestInfo Priority value of {0} did not match filter Priority value of {1}.", testRecord.TestInfo.Priority, filteringSettings.Priority);
            }
            else if (filteringSettings.SubArea != null && !filteringSettings.SubArea.Any(filterSubArea => (testRecord.TestInfo.SubArea ?? String.Empty).IndexOf(filterSubArea, StringComparison.OrdinalIgnoreCase) >= 0))
            {
                testRecord.FilteringExplanation = String.Format("TestInfo SubArea value of {0} did not match any of the filter SubAreas of {1}.", testRecord.TestInfo.SubArea, CommaSeparatedListExtensions.ToCommaSeparatedList(filteringSettings.SubArea));
            }
            else if (filteringSettings?.Versions?.Count() != 0 && !filteringSettings.Versions.Any(filterVersion => filteringSettings.VersionMatcher.VersionMatches(testRecord.TestInfo.Versions, filterVersion)))
            {
                testRecord.FilteringExplanation = String.Format("TestInfo Versions of {0} did not contain any values matching any of the filter Versions of {1}.", CommaSeparatedListExtensions.ToCommaSeparatedList(testRecord.TestInfo.Versions), CommaSeparatedListExtensions.ToCommaSeparatedList(filteringSettings.Versions));
            }
            else
            {
                testRecord.ExecutionEnabled = true;
            }
        }

        // See whether the configuration of machines that the testrecords are
        // set to run on are satisfactory.
        internal static void FilterConfigurations(TestRecord testRecord, DirectoryInfo testBinariesDirectory)
        {
            // If the test record is not enabled for execution (i.e. didn't pass
            // TestInfo filtering) then there is no need to see whether the machine
            // can run it or not, since we won't run it regardless. So only do
            // calculations if it is enabled.
            if (testRecord.ExecutionEnabled)
            {
                bool satisfied = false;
                //Tests with no Configuration Mixes automatically pass this filter.
                if (testRecord.TestInfo.Configurations != null && testRecord.TestInfo.Configurations.Count != 0)
                {
                    try
                    {
                        string cachedExplanation = null;
                        //Find a configuration mix which the machine satisfies, in order to run the test.
                        foreach (string configurationMix in testRecord.TestInfo.Configurations)
                        {
                            Configuration mix = Configuration.LoadConfigurationMix(configurationMix, testBinariesDirectory);
                            if (mix.IsSatisfied(testRecord.Machine))
                            {
                                satisfied = true;
                            }
                            else
                            {
                                cachedExplanation = mix.Explanation;
                            }
                        }
                        testRecord.ExecutionEnabled = satisfied;
                        if (satisfied != true)
                        {
                            testRecord.FilteringExplanation = "No machine Configuration could match this test's needs. On the last configuration:" + cachedExplanation;
                        }
                    }
                    catch (Exception e)
                    {
                        testRecord.ExecutionEnabled = false;
                        testRecord.FilteringExplanation = "Exception caught on Configuration Filtering - Omitting from run: " + e.ToString();
                    }
                }
            }
        }
    }
}
