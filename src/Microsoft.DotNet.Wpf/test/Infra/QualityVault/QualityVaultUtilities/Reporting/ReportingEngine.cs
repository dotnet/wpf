// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Xml;
using Microsoft.Test;
using System;

using System.Collections.Generic;


namespace Microsoft.Test.Reporting
{
    /// <summary>
    /// Provides a well factored access point for workflows to consume reporting
    /// </summary>
    internal static class ReportingEngine
    {
        /// <summary>
        /// Generates a complete set of static/non-interactive run reports for offline analysis.
        /// </summary>
        internal static void GenerateXmlReport(TestRecords tests, RunInfo runInfo, DirectoryInfo reportRoot, DirectoryInfo testBinRoot)
        {
            Profiler.StartMethod();

            ReportingUtilities.CreateScorchedDirectory(reportRoot);

            CopyStyleSheets(reportRoot, testBinRoot);

            TestRecords executedTests;
            TestRecords filteredTests;
            SeparateExecutedTests(tests, out executedTests, out filteredTests);

            //Note: Summary Report may well be superceded by the more elaborate Run Report...
            SummaryReportGenerator.Generate(executedTests, reportRoot);
            MachineSummaryReportGenerator.Generate(executedTests, reportRoot);
            VariationReportGenerator.Generate(executedTests, reportRoot);
            DrtReportGenerator.Generate(executedTests, reportRoot);
            InfraTrackingReportGenerator.Generate(executedTests, reportRoot);
            RunReportGenerator.Generate(executedTests, runInfo, reportRoot);
            XUnitReportGenerator.Generate(executedTests, reportRoot);

            FilteringReportGenerator.Generate(filteredTests, reportRoot);

            Profiler.EndMethod();
        }

        /// <summary>
        /// Partitions tests into two buckets: executed cases and filtered cases. Using out args to avoid doing same work twice.
        /// </summary>
        private static void SeparateExecutedTests(TestRecords tests, out TestRecords executedTests, out TestRecords filteredTests)
        {
            executedTests = new TestRecords();
            filteredTests = new TestRecords();
            foreach(TestRecord test in tests.TestCollection)
            {
                if (test.ExecutionEnabled)
                {
                    executedTests.TestCollection.Add(test);
                }
                else
                {
                    filteredTests.TestCollection.Add(test);
                }
            }
            executedTests.ExecutionGroupRecords = tests.ExecutionGroupRecords;
        }

        /// <summary>
        /// We need Style sheets to make XML reports Preetty.
        /// Right now, we store them as data, and copy them into report dir...
        /// </summary>
        private static void CopyStyleSheets(DirectoryInfo reportRoot, DirectoryInfo testBinRoot)
        {
            FileInfo[] fo = new DirectoryInfo(Path.Combine(testBinRoot.FullName, @"Infra\Reporting\")).GetFiles();
            foreach (FileInfo file in fo)
            {
                file.CopyTo(Path.Combine(reportRoot.FullName, file.Name));
            }
        }

        /// <summary>
        /// Produces a simple console summary report
        /// </summary>
        internal static void WriteSummaryToConsole(TestRecords results)
        {
            Console.WriteLine();
            Console.WriteLine("A total of {0} test Infos were processed, with the following results.", results.TestCollection.Count);
            if (results.TestCollection.Count > 0)
            {
                int Pass = 0;
                int Fail = 0;
                int FailWithBugID = 0;
                int Ignore = 0;

                #if REPORT_VERSIONS
                Dictionary<String, Tuple<int, List<TestInfo>>> dict = new Dictionary<String, Tuple<int, List<TestInfo>>>();
                #endif

                foreach (TestRecord test in results.TestCollection)
                {
                    TestInfo testInfo = test.TestInfo;

                    switch (ReportingUtilities.InterpretTestOutcome(test))
                    {
                        case Result.Ignore:
                            Ignore++;
                            break;

                        case Result.Pass:
                            Pass++;
                            break;

                        case Result.Fail:
                            Fail++;
                            if(ReportingUtilities.TestHasBugs(testInfo)) { FailWithBugID++;}
                            break;
                    }

                    #if REPORT_VERSIONS
                    String key = (testInfo.Versions == null)
                        ? String.Empty
                        : String.Join(",", ToArray(testInfo.Versions));

                    if (!dict.ContainsKey(key))
                    {
                        dict.Add(key, new Tuple<int, List<TestInfo>>(0, new List<TestInfo>()));
                    }

                    Tuple<int, List<TestInfo>> tuple = dict[key];
                    List<TestInfo> list = tuple.Item2;
                    if (list.Count < 5) list.Add(testInfo);
                    dict[key] = new Tuple<int, List<TestInfo>>(tuple.Item1 + 1, list);
                    #endif
                }

                Console.WriteLine(" Passed: {0}", Pass);
                Console.WriteLine(" Failed (need to analyze): {0}", Fail - FailWithBugID);
                Console.WriteLine(" Failed (with BugIDs): {0}", FailWithBugID);
                Console.WriteLine(" Ignore: {0}", Ignore);
                Console.WriteLine();

                #if REPORT_VERSIONS
                foreach (KeyValuePair<string, Tuple<int, List<TestInfo>>> kvp in dict)
                {
                    Tuple<int, List<TestInfo>> tuple = kvp.Value;
                    Console.WriteLine("{0} tests with versions '{1}'", tuple.Item1, kvp.Key);
                    foreach (TestInfo testInfo in tuple.Item2)
                    {
                        Console.WriteLine("   /Area={0} /SubArea={1} /Name={2}",
                            testInfo.Area, testInfo.SubArea, testInfo.Name);
                    }
                }
                #endif
            }
        }

        #if REPORT_VERSIONS
        private static string[] ToArray(System.Collections.ObjectModel.Collection<string> coll)
        {
            string[] result = new string[coll.Count];
            coll.CopyTo(result, 0);
            return result;
        }
        #endif
    }

}