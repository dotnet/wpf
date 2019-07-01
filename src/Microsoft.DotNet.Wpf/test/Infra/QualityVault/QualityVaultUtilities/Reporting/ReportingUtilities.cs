// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Xml;

namespace Microsoft.Test.Reporting
{
    /// <summary>
    /// Reporting specific utilities which have affinity to no specific report generator
    /// </summary>
    internal static class ReportingUtilities
    {
        internal static Dictionary<string, Dictionary<string, TestCollection>> GroupByArea(TestRecords tests)
        {
            Dictionary<string, Dictionary<string, TestCollection>> buckets = new Dictionary<string, Dictionary<string, TestCollection>>(StringComparer.OrdinalIgnoreCase);
            foreach (TestRecord test in tests.TestCollection)
            {
                string area = test.TestInfo.Area;
                if (String.IsNullOrEmpty(area)) { area = "None"; }

                Dictionary<string, TestCollection> areaDictionary;

                if (!buckets.ContainsKey(area))
                {
                    areaDictionary = new Dictionary<string, TestCollection>();
                    buckets.Add(area, areaDictionary);
                }
                else
                {
                    areaDictionary = buckets[area];
                }

                string subArea = test.TestInfo.SubArea;

                if (String.IsNullOrEmpty(subArea)) { subArea = "None"; }

                TestCollection subAreaCollection;

                if (!areaDictionary.ContainsKey(subArea))
                {
                    subAreaCollection = new TestCollection();
                    areaDictionary.Add(subArea, subAreaCollection);
                }
                else
                {
                    subAreaCollection = areaDictionary[subArea];
                }

                subAreaCollection.Add(test);
            }

            return buckets;
        }

        /// <summary>
        /// Create all directories for AreaReports
        /// </summary>
        /// <param name="directory"></param>
        internal static void CreateAreaReportsDirectories(DirectoryInfo directory)
        {
            CreateScorchedDirectory(directory);

            directory.CreateSubdirectory("TestLogs");
            directory.CreateSubdirectory("TestInfos");
        }
        /// <summary>
        /// If you've got to create a directory, but it might already have content, you should scorchify it.
        /// </summary>
        /// <param name="directory"></param>
        internal static void CreateScorchedDirectory(DirectoryInfo directory)
        {
            if (directory.Exists)
            {
                directory.Delete(true);
            }
            directory.Create();
            //TODO - Eat & log exceptions?
        }

        internal static Result InterpretTestOutcome(TestRecord test)
        {
            //Tests with no Pass/Fail results are ignored.
            Result result = Result.Ignore;

            foreach (VariationRecord variation in test.Variations)
            {
                //Any variation failures make the entire final test result a failure.
                if (variation.Result == Result.Fail)
                {
                    result = Result.Fail;
                }
                // Passing variations mixed with some ignores are considered passing.
                else if (variation.Result == Result.Pass && result == Result.Ignore)
                {
                    result = Result.Pass;
                }
            }
            return result;
        }

        /// <summary>
        /// This kind of policy is reporting centric, but is consumed within the Execution Engine.
        /// To use it in Reporting properly will require schema changes to account for Execution Groups.
        /// Once EG Records are implemented this dependency can be factored out to Reporting.
        /// </summary>        
        internal static void ApplyProcessCost(List<TestRecord> tests, TimeSpan processDuration)
        {
            TimeSpan totalSpentVariationTime = TimeSpan.Zero;
            foreach (TestRecord test in tests)
            {
                totalSpentVariationTime += GetTestVariationDurationSummation(test);
            }

            // This is the amount of time we amortize across each test, which was in running process outside of any particular variation
            TimeSpan perTestOverhead = TimeSpan.FromMilliseconds((processDuration - totalSpentVariationTime).TotalMilliseconds / (float)tests.Count);

            //Each test's duration == share of Process Overhead+ Summation of variation durations
            foreach (TestRecord test in tests)
            {
                TimeSpan localVariationTime = GetTestVariationDurationSummation(test);
                test.Duration = (localVariationTime + perTestOverhead);
            }
        }

        /// <summary>
        /// Provides summation of test variation durations. This is internal implementation/computation for ApplyProcessCost, not for general reporting use, as it omits process overhead.
        /// </summary>
        /// <param name="test"></param>
        /// <returns></returns>
        private static TimeSpan GetTestVariationDurationSummation(TestRecord test)
        {
            TimeSpan summation = TimeSpan.Zero;
            foreach (VariationRecord variation in test.Variations)
            {
                summation += Reporting.ReportingUtilities.GetVariationDuration(variation);
            }
            return summation;
        }

        internal static TimeSpan GetGroupDuration(ExecutionGroupRecord group)
        {
            if (group.EndTime != null && group.StartTime != null)
            {
                return group.EndTime.DateTime - group.StartTime.DateTime;
            }
            return TimeSpan.MinValue;
        }

        internal static TimeSpan GetTestDuration(TestRecord test)
        {
            if (test != null)
            {
                return test.Duration;
            }
            return TimeSpan.MinValue;
        }

        internal static TimeSpan GetVariationDuration(VariationRecord variation)
        {
            if (variation.EndTime != null && variation.StartTime != null)
            {
                return variation.EndTime.DateTime - variation.StartTime.DateTime;
            }
            return TimeSpan.MinValue;
        }
        /// <summary>
        /// Abstracts away the mechanism for converting a list of FileInfo based paths to string for report.
        /// </summary>
        internal static string ReportPaths(Collection<FileInfo> files)
        {
            string path = String.Empty;
            if (files != null && files.Count > 0)
            {
                path = files.First().DirectoryName;
            }
            return path;
        }

        /// <summary>
        /// Encapsulates logic for reporting Machine centric information.
        /// For now, this is just the machine name, but may change in future.
        /// </summary>
        /// <param name="machine"></param>
        /// <returns></returns>
        internal static string ReportMachine(MachineRecord machine)
        {
            if (machine != null)
            {
                return machine.Name;
            }
            else
            {
                return "[Did not Execute]";
            }
        }

        /// <summary>
        /// Format TimeSpan in total seconds.
        /// </summary>
        /// <param name="timeSpan"></param>
        /// <returns></returns>
        internal static string FormatTimeSpanAsSeconds(TimeSpan timeSpan)
        {
            return String.Format(CultureInfo.InvariantCulture, "{0:0.##}", timeSpan.TotalSeconds);
        }

        /// <summary>
        /// Create string representation of TimeSpan in format "hours:minutes:seconds".
        /// </summary>
        /// <param name="timeSpan">TimeSpan to format</param>
        /// <returns>Formated string</returns>
        internal static string FormatTimeSpanAsHms(TimeSpan timeSpan)
        {
            // We just need the integral part of TotalHours double. For example, 1.6 hour should be  
            // reported as 01:36:00, instead of 02:36:00. In the case TimeSpan is longer than a day, 
            // TimeSpan.Hours property is (TotalHourInInteger % 24). To avoid consider Days part, and to get the 
            // only integral part, Truncate is used for number of hours. 
            return String.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}:{2:00}",
                                 Math.Truncate(timeSpan.TotalHours),
                                 timeSpan.Minutes,
                                 timeSpan.Seconds);
        }
        /// <summary>
        /// Provides the Pass rate as a percentage string of 0-100%.
        /// </summary>
        /// <param name="tests"></param>
        /// <returns></returns>
        internal static string CalculatePassRate(TestRecords tests)
        {
            int failures = 0;
            int total = 0;
            foreach (TestRecord test in tests.TestCollection)
            {
                foreach (VariationRecord variation in test.Variations)
                {
                    failures += OneForFail(variation.Result);
                    total += OneForCountable(variation.Result);
                }
            }
            return String.Format("{0:0.00}%", (1 - (failures / (float)total)) * 100);
        }

        /// <summary>
        /// Returns 1 for results which affect pass rate
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        internal static int OneForCountable(Result result)
        {
            return (result != Result.Ignore) ? 1 : 0;
        }

        /// <summary>
        /// Returns 1 for failing results 
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        internal static int OneForFail(Result result)
        {
            return result == Result.Fail ? 1 : 0;
        }

        /// <summary>
        /// Returns 1 for failing results which have a known bug
        /// </summary>
        /// <param name="result"></param>
        /// <param name="hasBugs"></param>
        /// <returns></returns>
        internal static int OneForFailOnBug(Result result, bool hasBugs)
        {
            return (result == Result.Fail && hasBugs) ? 1 : 0;
        }

        /// <summary>
        /// Returns 1 for result that is ignored.
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        internal static int OneForIgnore(Result result)
        {
            return (result == Result.Ignore) ? 1 : 0;
        }

        /// <summary>
        /// Return true if test has bugs associated. 
        /// </summary>
        /// <param name="testInfo">TestInfo</param>
        /// <returns>true if test has bugs, false otherwise</returns>
        internal static bool TestHasBugs(TestInfo testInfo)
        {
            return testInfo.Bugs != null && testInfo.Bugs.Count > 0;
        }

        /// <summary>
        /// Returns truncated Log string.
        /// </summary>
        /// <param name="log">Log</param>
        /// <param name="logFileName">Name of the file to save long log</param>
        /// /// <param name="logTruncated"> whether the log has been truncated.</param>
        /// <returns>Truncated Log</returns>
        internal static string ProcessLongLog(string log, string logFileName, ref bool logTruncated)
        {
            if (!String.IsNullOrEmpty(log) && log.Length > MaxLogLength)
            {
                using (StreamWriter sw = new StreamWriter(logFileName))
                {
                    sw.Write(log);
                    sw.Flush();
                }
                log = String.Format("{0}\n{1}\n...\n{2}",
                "Log was too long, thus truncated.",
                log.Substring(0, 300),
                log.Substring(log.Length - 1000));
                logTruncated = true;
            }

            return log;
        }

        internal static void SaveTestInfo(string fileName, TestInfo test)
        {
            FileInfo fileInfo = new FileInfo(fileName);
            using (XmlTextWriter textWriter = new XmlTextWriter(fileInfo.Open(FileMode.Create, FileAccess.Write), System.Text.Encoding.UTF8))
            {
                textWriter.Formatting = Formatting.Indented;
                ObjectSerializer.Serialize(textWriter, test);
            }
        }
        internal static readonly string TestInfosDir = "TestInfos";
        internal static readonly string TestLogsDir = "TestLogs";
        internal static readonly int MaxLogLength = 8000;
    }
}