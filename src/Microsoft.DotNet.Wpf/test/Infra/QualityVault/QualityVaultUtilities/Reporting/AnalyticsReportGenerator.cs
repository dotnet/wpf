// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Xml;
using Microsoft.Test;


namespace Microsoft.Test.Reporting
{

    /// <summary>
    /// Generates feature infrastructure feature use reports.
    /// Purpose: Provide data about infrastructure feature use to help plan and track adoption of new improvements to the infra.
    /// Target Users: Infra team
    ///               Secondary customers may be leads, advanced IC's and lab, when discussing new features.    
    /// </summary>    
    internal static class InfraTrackingReportGenerator
    {
        internal static void Generate(TestRecords results, DirectoryInfo ReportRoot)
        {
            string path = Path.Combine(ReportRoot.FullName, "InfraTrackingReport.xml");
            using (XmlTableWriter tableWriter = new XmlTableWriter(path))
            {                
                tableWriter.AddXsl("InfraTrackingReport.xsl");
                tableWriter.WriteStartElement("TestFeatureUsage");
                foreach (TestRecord test in results.TestCollection)
                {
                    TestInfo testInfo = test.TestInfo;
                    tableWriter.WriteStartElement("Test");
                    tableWriter.WriteAttributeString("Area", testInfo.Area + " " + testInfo.SubArea);
                    tableWriter.WriteAttributeString("Test", testInfo.Name);
                    
                    if (testInfo.Bugs != null)
                    {
                        tableWriter.WriteAttributeString("Bugs", testInfo.Bugs.ToCommaSeparatedList());
                    }
                    if (testInfo.Configurations != null)
                    {
                        tableWriter.WriteAttributeString("Configurations", testInfo.Configurations.ToCommaSeparatedList());
                    }
                    if (testInfo.Deployments != null)
                    {
                        tableWriter.WriteAttributeString("Deployments", testInfo.Deployments.ToCommaSeparatedList());
                    }
                    if (testInfo.ExecutionGroup != null)
                    {
                        tableWriter.WriteAttributeString("ExecutionGroup", testInfo.ExecutionGroup);
                    }
                    tableWriter.WriteEndElement();
                }
                tableWriter.WriteEndElement();
            }
        }        
    }
}