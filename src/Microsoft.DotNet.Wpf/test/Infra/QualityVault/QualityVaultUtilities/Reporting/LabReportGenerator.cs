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
    internal static class RunReportGenerator
    {
        internal static void Generate(TestRecords tests, RunInfo runInfo, DirectoryInfo reportRoot)
        {
            using (XmlTableWriter tableWriter = new XmlTableWriter(Path.Combine(reportRoot.FullName, "LabReport.xml")))
            {
                tableWriter.AddXsl("LabReport.xsl");
                tableWriter.WriteStartElement("LabReport");

                string reportPath = reportRoot.FullName;
                if (reportPath.EndsWith(@"\"))
                {
                    // Remove the final slashy to prevent malformed XML since all XSLs expect non-slash end.
                    reportPath = reportPath.Remove(reportPath.Length - 1);
                }
                tableWriter.WriteAttributeString("ReportPath", reportPath);

                SummaryReportGenerator.WriteSummaryReport(tableWriter, tests);
                AppendNotes(tableWriter, runInfo);
                AppendConfigurations(tableWriter, runInfo);
                AppendPaths(tableWriter, runInfo, reportRoot);
                tableWriter.WriteEndElement();
            }
        }

        private static void AppendNotes(XmlTableWriter tableWriter, RunInfo runInfo)
        {
            tableWriter.WriteStartElement("Notes");
            tableWriter.WriteEndElement();
        }

        private static void AppendConfigurations(XmlTableWriter tableWriter, RunInfo runInfo)
        {
            tableWriter.WriteStartElement("Configurations");

            tableWriter.WriteKeyValuePair("Run ID", runInfo.Id);
            tableWriter.WriteKeyValuePair("Build", runInfo.Build);
            tableWriter.WriteKeyValuePair("Branch", runInfo.Branch);
            tableWriter.WriteKeyValuePair("Platform", runInfo.OS);
            tableWriter.WriteKeyValuePair("Language", runInfo.Language);
            tableWriter.WriteKeyValuePair("Priorities", runInfo.Priority);
            tableWriter.WriteKeyValuePair("Run Type", runInfo.RunType);
            tableWriter.WriteKeyValuePair("Date", runInfo.Date);
            tableWriter.WriteKeyValuePair("Architecture", runInfo.Architecture);
            tableWriter.WriteKeyValuePair("IE Version", runInfo.IEVersion);
            tableWriter.WriteKeyValuePair("DPI", runInfo.Dpi);
            tableWriter.WriteKeyValuePair("Versions", runInfo.Version);
            tableWriter.WriteKeyValuePair("Code Coverage", runInfo.IsCodeCoverage);
            tableWriter.WriteKeyValuePair("AppVerify", runInfo.IsAppVerify);
            tableWriter.WriteKeyValuePair("Name", runInfo.Name);

            tableWriter.WriteEndElement();
        }

        private static void AppendPaths(XmlTableWriter tableWriter, RunInfo runInfo, DirectoryInfo reportRoot)
        {
            tableWriter.WriteStartElement("Paths");
            tableWriter.WriteKeyValuePair("Results", reportRoot.FullName);
            tableWriter.WriteKeyValuePair("Build Location:", runInfo.InstallerPath);
            tableWriter.WriteKeyValuePair("Test Build Path", runInfo.TestBinariesPath);
            tableWriter.WriteEndElement();
        }
    }
}