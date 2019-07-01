// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Microsoft.Test.Reporting
{
    /// <summary>
    /// Generates the Summary Report Page. 
    /// Purpose: This page provides overall pass rate and execution time stats about the run, grouped by feature. It should be simple, and easy to quickly interpret.
    /// Target Users: Breadth of test org - Leads, IC's, lab and infra team
    /// </summary>
    internal static class SummaryReportGenerator
    {
        internal static void Generate(TestRecords tests, DirectoryInfo ReportRoot)
        {
            using (XmlTableWriter tableWriter = new XmlTableWriter(Path.Combine(ReportRoot.FullName, "Summary.xml")))
            {
                tableWriter.AddXsl("Summary.xsl");
                WriteSummaryReport(tableWriter, tests);
            }
        }

        internal static void WriteSummaryReport(XmlTableWriter tableWriter, TestRecords tests)
        {
            SortedDictionary<string, AreaSummaryEntry> SummaryTable = ProduceAreaSummaries(tests);
            AddTotalsLine(SummaryTable);

            tableWriter.WriteStartElement("Summary");
            foreach (AreaSummaryEntry entry in SummaryTable.Values)
            {
                tableWriter.WriteStartElement("AreaSummary");
                tableWriter.WriteAttributeString("AreaName", entry.Name);
                tableWriter.WriteAttributeString("FailingVariations", entry.FailedVariations.ToString(CultureInfo.InvariantCulture));
                tableWriter.WriteAttributeString("FailedVariationsWithBugs", entry.FailedVariationsWithBugs.ToString(CultureInfo.InvariantCulture));
                tableWriter.WriteAttributeString("IgnoredVariations", entry.IgnoredVariations.ToString(CultureInfo.InvariantCulture));
                tableWriter.WriteAttributeString("TotalVariations", (entry.TotalVariations).ToString(CultureInfo.InvariantCulture));
                float passRate = 0;
                float adjustedPassRate = 0;
                if (entry.TotalVariations > 0)
                {
                    passRate = (((entry.TotalVariations - entry.FailedVariations) / (float)entry.TotalVariations) * 100);
                    //Failures on tests with known bugs can be treated as passing, but we make clear this is not the actual pass rate.
                    adjustedPassRate = (((entry.TotalVariations - entry.FailedVariations + entry.FailedVariationsWithBugs) / (float)entry.TotalVariations) * 100);
                }
                tableWriter.WriteAttributeString("PassRate", passRate.ToString("0.00", CultureInfo.InvariantCulture));
                tableWriter.WriteAttributeString("AdjustedPassRate", adjustedPassRate.ToString("0.00", CultureInfo.InvariantCulture));
                tableWriter.WriteAttributeString("TestExecutionTime", ReportingUtilities.FormatTimeSpanAsHms(entry.TestExecutionTime));
                tableWriter.WriteAttributeString("TotalExecutionTime", ReportingUtilities.FormatTimeSpanAsHms(entry.TotalExecutionTime));

                tableWriter.WriteEndElement();
            }
            tableWriter.WriteEndElement();
        }

        internal static SortedDictionary<string, AreaSummaryEntry> ProduceAreaSummaries(TestRecords tests)
        {
            SortedDictionary<string, AreaSummaryEntry> SummaryTable = new SortedDictionary<string, AreaSummaryEntry>(StringComparer.OrdinalIgnoreCase);

            //Go through VariationRecords in each test to build up SummaryStats
            foreach (TestRecord test in tests.TestCollection)
            {
                AreaSummaryEntry entry;
                string area = test.TestInfo.Area;

                //Create Entry if area doesn't exist yet, else load it up
                if (!SummaryTable.ContainsKey(area))
                {
                    entry = new AreaSummaryEntry();
                    entry.TestExecutionTime = new TimeSpan();
                    entry.Name = area;
                    entry.AssociatedTests = new TestCollection();
                    SummaryTable.Add(area, entry);
                }
                else
                {
                    entry = SummaryTable[area];
                }
                entry.AssociatedTests.Add(test);
                bool hasBugs = (test.TestInfo.Bugs != null && test.TestInfo.Bugs.Count > 0);
                foreach (VariationRecord variation in test.Variations)
                {
                    entry.TotalVariations += ReportingUtilities.OneForCountable(variation.Result);                    
                    entry.FailedVariations += ReportingUtilities.OneForFail(variation.Result);
                    entry.IgnoredVariations += ReportingUtilities.OneForIgnore(variation.Result);
                    entry.FailedVariationsWithBugs += ReportingUtilities.OneForFailOnBug(variation.Result, hasBugs);                  
                }
                entry.TestExecutionTime += test.Duration;
            }

            foreach (ExecutionGroupRecord group in tests.ExecutionGroupRecords)
            {
                AreaSummaryEntry entry;
                string area = group.Area;                
                //Assumption - all areas have been defined by the list of tests scanned in above loop
                entry = SummaryTable[area];

                entry.TotalExecutionTime += ReportingUtilities.GetGroupDuration(group);
                // Take the earliest start time as the start of the entire area
                if (entry.StartTime < group.StartTime.DateTime)
                {
                    entry.StartTime = group.StartTime.DateTime;
                }

                // Take the last end time as the end of the entire area
                if (group.EndTime.DateTime > entry.EndTime)
                {
                    entry.EndTime = group.EndTime.DateTime;
                }
            }

            return SummaryTable;
        }

        private static void AddTotalsLine(SortedDictionary<string, AreaSummaryEntry> SummaryTable)
        {
            AreaSummaryEntry totals = new AreaSummaryEntry();
            totals.Name = "Total";
            foreach (AreaSummaryEntry entry in SummaryTable.Values)
            {
                totals.TotalVariations += entry.TotalVariations;
                totals.FailedVariations += entry.FailedVariations;
                totals.IgnoredVariations += entry.IgnoredVariations;
                totals.FailedVariationsWithBugs += entry.FailedVariationsWithBugs;
                totals.TestExecutionTime += entry.TestExecutionTime;
                totals.TotalExecutionTime += entry.TotalExecutionTime;
            }
            SummaryTable.Add(totals.Name, totals);
        }

        internal class AreaSummaryEntry
        {
            public string Name;
            public int TotalVariations;
            public int FailedVariations;
            public int IgnoredVariations;              
            public int FailedVariationsWithBugs;
            public TimeSpan TestExecutionTime;
            public TimeSpan TotalExecutionTime;
            public DateTime StartTime = DateTime.MinValue;
            public DateTime EndTime = DateTime.MinValue;
            public TestCollection AssociatedTests;
        }

    }
}