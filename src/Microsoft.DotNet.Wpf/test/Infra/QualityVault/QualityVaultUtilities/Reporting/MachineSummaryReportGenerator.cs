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
    /// Generates the Machine Summary Report Page. 
    /// Purpose: This page provides overall pass rate and execution time stats about the run, grouped by machine.
    /// Target Users: Lab
    /// </summary>
    internal static class MachineSummaryReportGenerator
    {
        internal static void Generate(TestRecords tests, DirectoryInfo ReportRoot)
        {
            using (XmlTableWriter tableWriter = new XmlTableWriter(Path.Combine(ReportRoot.FullName, "MachineSummary.xml")))
            {
                tableWriter.AddXsl("MachineSummary.xsl");
                WriteSummaryReport(tableWriter, tests);
            }
        }

        internal static void WriteSummaryReport(XmlTableWriter tableWriter, TestRecords tests)
        {
            // Most of this logic overlaps with SummaryReportGenerator. A common parent or templated helper may resolve the duplication.
            Dictionary<string, MachineSummaryEntry> SummaryTable = ProduceMachineSummaries(tests);

            tableWriter.WriteStartElement("Summary");
            foreach (MachineSummaryEntry entry in SummaryTable.Values)
            {
                tableWriter.WriteStartElement("MachineSummary");
                tableWriter.WriteAttributeString("MachineName", entry.Name);
                tableWriter.WriteAttributeString("FailingVariations", entry.FailedVariations.ToString(CultureInfo.InvariantCulture));
                tableWriter.WriteAttributeString("TotalVariations", (entry.TotalVariations).ToString(CultureInfo.InvariantCulture));
                tableWriter.WriteAttributeString("TestsWithoutVariation", (entry.TestsWithoutVariation).ToString(CultureInfo.InvariantCulture));
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

                tableWriter.WriteEndElement();
            }
            tableWriter.WriteEndElement();
        }

        private static Dictionary<string, MachineSummaryEntry> ProduceMachineSummaries(TestRecords tests)
        {
            Dictionary<string, MachineSummaryEntry> SummaryTable = new Dictionary<string, MachineSummaryEntry>(StringComparer.OrdinalIgnoreCase);

            //Go through VariationRecords in each test to build up SummaryStats
            foreach (TestRecord test in tests.TestCollection)
            {
                MachineSummaryEntry entry;
                string name = ReportingUtilities.ReportMachine(test.Machine);

                //Create Entry if machine doesn't exist yet, else load it up
                if (!SummaryTable.ContainsKey(name))
                {
                    entry = new MachineSummaryEntry();
                    entry.TestExecutionTime = TimeSpan.Zero;
                    entry.Name = name;
                    SummaryTable.Add(name, entry);
                }
                else
                {
                    entry = SummaryTable[name];
                }
                bool hasBugs = (test.TestInfo.Bugs != null && test.TestInfo.Bugs.Count > 0);
                foreach (VariationRecord variation in test.Variations)
                {
                    entry.TotalVariations += ReportingUtilities.OneForCountable(variation.Result);
                    entry.FailedVariations += ReportingUtilities.OneForFail(variation.Result);
                    entry.FailedVariationsWithBugs += ReportingUtilities.OneForFailOnBug(variation.Result, hasBugs);                    
                }
                entry.TestExecutionTime += ReportingUtilities.GetTestDuration(test);                
                
                if(test.Variations.Count == 0)
                {
                    entry.TestsWithoutVariation += 1;
                }
            }
            return SummaryTable;
        }

        private class MachineSummaryEntry
        {
            public string Name;
            public int TotalVariations;
            public int FailedVariations;
            public int FailedVariationsWithBugs;
            public int TestsWithoutVariation;
            public TimeSpan TestExecutionTime;
        }

    }
}