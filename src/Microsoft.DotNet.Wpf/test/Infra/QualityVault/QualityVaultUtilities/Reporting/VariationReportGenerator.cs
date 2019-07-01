// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Collections;

namespace Microsoft.Test.Reporting
{
    /// <summary>
    /// Generates feature specific variation reports.
    /// Purpose: This view of the data is targeted to test IC's for failure investigation.
    ///          As such, this view provides granular details about every test variation in that feature.
    ///          This report can puts more emphasis on detailed verbosity over simplicity, to aid in investigations.
    /// Target Users: Test IC's
    /// </summary>
    internal static class VariationReportGenerator
    {
        internal static void Generate(TestRecords tests, DirectoryInfo ReportRoot)
        {

            DirectoryInfo areasDirectory = new DirectoryInfo(Path.Combine(ReportRoot.FullName, "AreaReports"));
            ReportingUtilities.CreateAreaReportsDirectories(areasDirectory);

            Dictionary<string, Dictionary<string, TestCollection>> areas = ReportingUtilities.GroupByArea(tests);
            foreach (KeyValuePair<string, Dictionary<string, TestCollection>> bucket in areas)
            {
                string area = bucket.Key;
                Dictionary<string, TestCollection> areaDictionary = bucket.Value;

                Generate(areaDictionary, areasDirectory.FullName, area);
            }
        }

        private static void Generate(Dictionary<string, TestCollection> testCollectionDictionary, string areaReportsPath, string area)
        {
            string filenameSuffix = "VariationReport.xml";

            using (XmlTableWriter tableWriter = new XmlTableWriter(Path.Combine(areaReportsPath, area + filenameSuffix)))
            {
                tableWriter.AddXsl(@"..\VariationReport.xsl");
                tableWriter.WriteStartElement("Variations");

                int testIndex = 0;
                foreach (string subarea in testCollectionDictionary.Keys)
                {
                    TestCollection testsInSubarea = testCollectionDictionary[subarea] as TestCollection;
                    tableWriter.WriteStartElement("SubArea");
                    tableWriter.WriteAttributeString("SubArea", subarea);
                    foreach (TestRecord test in testsInSubarea)
                    {
                        testIndex++;
                        WriteTestNode(tableWriter, areaReportsPath, area, test, testIndex);
                    }

                    tableWriter.WriteEndElement();
                }
            }
        }


        /// <summary>
        ///  Write one test.
        /// </summary>
        /// <param name="tableWriter">Writer</param>
        /// <param name="areaReportsPath">Area path</param>
        /// <param name="area">Area name</param>
        /// <param name="test">TestRecord</param>
        /// <param name="testIndex">Index of the test</param>
        private static void WriteTestNode(XmlTableWriter tableWriter, string areaReportsPath, string area, TestRecord test, int testIndex)
        {
            TestInfo testInfo = test.TestInfo;

            string testLogsDirectory = Path.Combine(areaReportsPath, ReportingUtilities.TestLogsDir);
            string testInfosDirectory = Path.Combine(areaReportsPath, ReportingUtilities.TestInfosDir);

            //Test Layer with attribute needed for all variation.
            tableWriter.WriteStartElement("Test");
            tableWriter.WriteAttributeString("Name", Escape(testInfo.Name));
            tableWriter.WriteAttributeString("KnownBugs", test.TestInfo.Bugs.ToCommaSeparatedList());
            tableWriter.WriteAttributeString("Priority", test.TestInfo.Priority.ToString());
            tableWriter.WriteAttributeString("Machine", ReportingUtilities.ReportMachine(test.Machine));

            string testInfoPath = Path.Combine(testInfosDirectory, String.Format("{0}_{1}.xml", area, testIndex));

            if (ReportingUtilities.InterpretTestOutcome(test) == Result.Fail)
            {
                ReportingUtilities.SaveTestInfo(testInfoPath, testInfo);
                tableWriter.WriteAttributeString("TestInfo", Path.Combine(ReportingUtilities.TestInfosDir, Path.GetFileName(testInfoPath)));
            }

            for (int variationIndex = 0; variationIndex < test.Variations.Count; variationIndex++)
            {
                VariationRecord variation = test.Variations[variationIndex];

                string variationLogPath = Path.Combine(testLogsDirectory, String.Format("{0}_{1}_{2}.log", area, testIndex, variationIndex));
                WriteVariationNode(tableWriter, variation, variationLogPath);
            }

            //SEMI HACK: Create a node to hold information for test, that are not needed for most variation.
            // This solves three problems:
            //           1 - Allows us to provide Test level information in tabular form
            //           2 - Allows us to include Test level execution logs in these reports
            // Note - To nest these things attribute in parent Test node, while less hacky, would create a different semantic from the perspective of any XML viewer, which would be bad. very bad.
            string testLogPath = Path.Combine(testLogsDirectory, String.Format("{0}_{1}.log", area, testIndex));
            WriteChildTestNode(tableWriter, test, testInfo, testLogPath);
            tableWriter.WriteEndElement();
        }

        /// <summary>
        /// Write a Test node under Test, to hold information for the Test: log, test result, duration, Test info, which
        /// won't show for every Variation in Xml viewer.
        /// </summary>
        /// <param name="tableWriter">Writer</param>
        /// <param name="test">test</param>
        /// <param name="testInfo">testInfo</param>
        /// <param name="testLogPath">test log path</param>
        private static void WriteChildTestNode(XmlTableWriter tableWriter, TestRecord test, TestInfo testInfo, string testLogPath)
        {
            bool logTruncated = false;
            tableWriter.WriteStartElement("Test");
            tableWriter.WriteAttributeString("Duration", ReportingUtilities.FormatTimeSpanAsSeconds(ReportingUtilities.GetTestDuration(test))); //Total test execution Time
            tableWriter.WriteAttributeString("Result", ReportingUtilities.InterpretTestOutcome(test).ToString());
            tableWriter.WriteAttributeString("Log", ReportingUtilities.ProcessLongLog(test.Log, testLogPath, ref logTruncated));
            if (logTruncated) { tableWriter.WriteAttributeString("LogPath", Path.Combine(ReportingUtilities.TestLogsDir, Path.GetFileName(testLogPath))); }
            tableWriter.WriteAttributeString("LogDir", ReportingUtilities.ReportPaths(test.LoggedFiles));

            int failed = 0;
            int failedOnBug = 0;
            bool hasBugs = ReportingUtilities.TestHasBugs(testInfo);
            foreach (VariationRecord variation in test.Variations)
            {
                failed += ReportingUtilities.OneForFail(variation.Result);
                failedOnBug += ReportingUtilities.OneForFailOnBug(variation.Result, hasBugs);
            }
            tableWriter.WriteAttributeString("Failures", string.Format("{0}", failed - failedOnBug));
            tableWriter.WriteAttributeString("Total", string.Format("{0}", test.Variations.Count));
            tableWriter.WriteEndElement();
        }

        private static void WriteVariationNode(XmlTableWriter tableWriter, VariationRecord variation, string variationLogPath)
        {
            bool logTruncated = false;

            tableWriter.WriteStartElement("Variation");
            tableWriter.WriteAttributeString("Variation", variation.VariationName);
            tableWriter.WriteAttributeString("VariationId", variation.VariationId.ToString(CultureInfo.InvariantCulture));
            tableWriter.WriteAttributeString("Duration", ReportingUtilities.FormatTimeSpanAsSeconds(ReportingUtilities.GetVariationDuration(variation)));
            tableWriter.WriteAttributeString("Result", variation.Result.ToString());
            tableWriter.WriteAttributeString("Log", ReportingUtilities.ProcessLongLog(variation.Log, variationLogPath, ref logTruncated));
            if (logTruncated) { tableWriter.WriteAttributeString("LogPath", Path.Combine(ReportingUtilities.TestLogsDir, Path.GetFileName(variationLogPath))); }
            tableWriter.WriteAttributeString("LogDir", ReportingUtilities.ReportPaths(variation.LoggedFiles));
            tableWriter.WriteEndElement();
        }

        // add escape characters to a test name so that it can be pasted into a command line
        private static string Escape(string s)
        {
            return s.Replace(@"\", @"\\").Replace(@",", @"\,");
        }
    }
}