// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.Test.Reporting
{
    /// <summary>
    /// Generates product-wide variation report for DRT's.
    /// Purpose: This view provides a simple overview of all DRT results for rapid investigation.
    /// Target Users: Devs
    /// </summary>    
    internal static class DrtReportGenerator
    {

        internal static void Generate(TestRecords records, DirectoryInfo ReportRoot)
        {
            TestRecords drtVariations = FilterDrts(records);
            string path = Path.Combine(ReportRoot.FullName, "DrtReport.xml");

            //Only produce this view if DRT's were executed.
            if (drtVariations.TestCollection.Count > 0)
            {
                Generate(drtVariations, path);
            }
        }

        internal static TestRecords FilterDrts(TestRecords records)
        {
            IEnumerable<TestRecord> drts = records.TestCollection.Where(test => (test.TestInfo.Type == TestType.DRT));
            return new TestRecords(drts);
        }

        internal static TestCollection FilterNonPassingTests(TestRecords tests)
        {
            IEnumerable<TestRecord> failingTests = tests.TestCollection.Where(test => (ReportingUtilities.InterpretTestOutcome(test) != Result.Pass));
            return new TestCollection(failingTests);
        }

        private static void Generate(TestRecords records, string path)
        {
            using (XmlTableWriter tableWriter = new XmlTableWriter(path))
            {
                tableWriter.AddXsl(@"DrtReport.xsl");
                tableWriter.WriteStartElement("Variations");
                tableWriter.WriteAttributeString("PassRate", ReportingUtilities.CalculatePassRate(records));
                foreach (TestRecord test in FilterNonPassingTests(records))
                {
                    TestInfo testInfo = test.TestInfo;
                    {
                        tableWriter.WriteStartElement("Variation");
                        tableWriter.WriteAttributeString("Area", testInfo.Area);
                        tableWriter.WriteAttributeString("TestName", testInfo.Name);
                        tableWriter.WriteAttributeString("Variation", "Test Level Summary");
                        tableWriter.WriteAttributeString("Duration", ReportingUtilities.FormatTimeSpanAsSeconds(ReportingUtilities.GetTestDuration(test))); //Total test execution Time                        
                        tableWriter.WriteAttributeString("Result", ReportingUtilities.InterpretTestOutcome(test).ToString());
                        tableWriter.WriteAttributeString("Log", test.Log);
                        tableWriter.WriteAttributeString("LogDir", ReportingUtilities.ReportPaths(test.LoggedFiles));
                        tableWriter.WriteEndElement();
                    }

                    foreach (VariationRecord variation in test.Variations)
                    {

                        tableWriter.WriteStartElement("Variation");
                        tableWriter.WriteAttributeString("Area", testInfo.Area);
                        tableWriter.WriteAttributeString("TestName", testInfo.Name);
                        tableWriter.WriteAttributeString("Variation", variation.VariationName);
                        tableWriter.WriteAttributeString("Duration", ReportingUtilities.FormatTimeSpanAsSeconds(ReportingUtilities.GetVariationDuration(variation)));
                        tableWriter.WriteAttributeString("Result", variation.Result.ToString());
                        tableWriter.WriteAttributeString("Log", variation.Log);
                        tableWriter.WriteAttributeString("LogDir", ReportingUtilities.ReportPaths(variation.LoggedFiles));
                        tableWriter.WriteEndElement();
                    }
                }
                tableWriter.WriteEndElement();
            }
        }
    }
}