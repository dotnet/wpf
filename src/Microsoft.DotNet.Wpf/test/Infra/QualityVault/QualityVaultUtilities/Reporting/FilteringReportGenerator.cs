// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace Microsoft.Test.Reporting
{
    /// <summary>
    /// Generates filtering report for understanding why a test was not executed.    
    /// Target Users: Testers
    /// </summary>    
    internal static class FilteringReportGenerator
    {

        internal static void Generate(TestRecords records, DirectoryInfo ReportRoot)
        {            
            string path = Path.Combine(ReportRoot.FullName, "FilteringReport.xml");
            Generate(records, path);
        }

        private static void Generate(TestRecords Records, string path)
        {
            using (XmlTableWriter tableWriter = new XmlTableWriter(path))
            {
                tableWriter.AddXsl(@"FilteringReport.xsl");
                tableWriter.WriteStartElement("Tests");
                foreach (TestRecord test in Records.TestCollection)
                {
                    TestInfo testInfo = test.TestInfo;
                    {
                        tableWriter.WriteStartElement("Test");
                        tableWriter.WriteAttributeString("Area", testInfo.Area);
                        tableWriter.WriteAttributeString("SubArea", testInfo.SubArea);
                        tableWriter.WriteAttributeString("TestName", testInfo.Name);
                        tableWriter.WriteAttributeString("Explanation", test.FilteringExplanation);
                        tableWriter.WriteEndElement();
                    }
                }
                tableWriter.WriteEndElement();
            }
        }
    }
}