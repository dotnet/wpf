// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Linq;
using Microsoft.Test;
using System;
using System.Text;

namespace Microsoft.Test.Reporting
{

    internal static class XUnitReportGenerator
    {

        static XUnitReportGenerator()
        {
            HelixResultsContainerUri = System.Environment.GetEnvironmentVariable("HELIX_RESULTS_CONTAINER_URI");
            HelixResultsContainerRsas = System.Environment.GetEnvironmentVariable("HELIX_RESULTS_CONTAINER_RSAS");
        }

        internal static void Generate(TestRecords records, DirectoryInfo ReportRoot)
        {
            // See https://xunit.net/docs/format-xml-v2.html for the documentation on how to format xunit logs
            var root = new XElement("assemblies");

            // We run our tests by Area in Helix. The SummaryReportGenerator does a nice job of aggregating the TestRecords
            // by area already, so we'l reuse that. This ensures we aren't making too many assummptions (like that all records are in the same Area)
            // that could break later.
            var resultsByArea = SummaryReportGenerator.ProduceAreaSummaries(records);
            foreach (SummaryReportGenerator.AreaSummaryEntry areaEntry in resultsByArea.Values)
            {
                string assemblyName = areaEntry.AssociatedTests.FirstOrDefault()?.TestInfo.DriverParameters["exe"];
                var assembly = new XElement("assembly");
                assembly.SetAttributeValue("name", assemblyName);
                assembly.SetAttributeValue("test-framework", "QualityVault");
                assembly.SetAttributeValue("run-date", DateTime.Now.ToString("yyyy-mm-dd"));
                assembly.SetAttributeValue("run-time", areaEntry.StartTime.ToString(@"hh\:mm\:ss"));
                assembly.SetAttributeValue("time", (areaEntry.EndTime - areaEntry.StartTime).TotalSeconds);
                assembly.SetAttributeValue("total", areaEntry.TotalVariations);
                assembly.SetAttributeValue("passed", areaEntry.TotalVariations - areaEntry.FailedVariations - areaEntry.IgnoredVariations);
                assembly.SetAttributeValue("failed", areaEntry.FailedVariations);
                assembly.SetAttributeValue("skipped", areaEntry.IgnoredVariations);
                assembly.SetAttributeValue("errors", 0);
                root.Add(assembly);

                foreach (TestRecord testRecord in areaEntry.AssociatedTests)
                {
                    var collection = new XElement("collection");
                    int testPassedCount = testRecord.Variations.Where(variation => variation.Result == Result.Pass).Count();
                    int testFailedCount = testRecord.Variations.Where(variation => variation.Result == Result.Fail).Count();
                    int testSkippedCount = testRecord.Variations.Where(variation => variation.Result == Result.Ignore).Count();

                    collection.SetAttributeValue("total", testRecord.Variations.Count);
                    collection.SetAttributeValue("passed", testPassedCount);
                    collection.SetAttributeValue("failed", testFailedCount);
                    collection.SetAttributeValue("skipped", testSkippedCount);
                    collection.SetAttributeValue("name", testRecord.TestInfo.Name);
                    collection.SetAttributeValue("time", ReportingUtilities.FormatTimeSpanAsSeconds(ReportingUtilities.GetTestDuration(testRecord)));
                    assembly.Add(collection);

                    foreach (VariationRecord variation in testRecord.Variations)
                    {
                        var test = new XElement("test");
                        string className = testRecord.TestInfo.DriverParameters["class"];
                        string methodName = testRecord.TestInfo.DriverParameters["method"];

                        test.SetAttributeValue("type", className);
                        test.SetAttributeValue("method", methodName);

                        test.SetAttributeValue("name", variation.VariationName);
                        test.SetAttributeValue("time", testRecord.Duration.TotalSeconds);
                        test.SetAttributeValue("result", variation.Result == Result.Ignore ? "Skip" : variation.Result.ToString());

                        if (variation.Result != Result.Pass)
                        {
                            var failure = new XElement("failure");
                            failure.SetAttributeValue("exception-type", "Exception");

                            var message = new XElement("message");

                            StringBuilder errorMessage = new StringBuilder();

                            errorMessage.AppendLine("Error Log: ");
                            errorMessage.AppendLine(testRecord.Log);

                            message.Add(new XCData(errorMessage.ToString()));
                            failure.Add(message);

                            test.Add(failure);
                        }
                        collection.Add(test);
                    }

                }
            }


            string xunitOutputPath = Path.Combine(ReportRoot.FullName, "testResults.xml");
            File.WriteAllText(xunitOutputPath, root.ToString());
        }

        private static string GetUploadedFileUrl(string filePath)
        {
            var filename = Path.GetFileName(filePath);
            return string.Format("{0}/{1}{2}", HelixResultsContainerUri, filename, HelixResultsContainerRsas);
        }

        private static string HelixResultsContainerUri;
        private static string HelixResultsContainerRsas;

    }
}