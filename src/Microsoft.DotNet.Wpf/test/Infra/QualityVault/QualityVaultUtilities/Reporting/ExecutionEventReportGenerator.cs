// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Globalization;
using System.IO;
using Microsoft.Test.Reporting;

internal static class ExecutionEventReportGenerator
{
    /// <summary>
    /// Store the Event Log to Disk
    /// Target Users: Infra, Lab, IC's doing deep investigations
    /// </summary>
    internal static void Generate(EventLogEntryCollection infraLogEntries, DirectoryInfo reportRoot)
    {
        string path = Path.Combine(reportRoot.FullName, "ExecutionEventReport.xml");
        using (XmlTableWriter tableWriter = new XmlTableWriter(path))
        {
            //This relative path only works in local scenario... :(
            tableWriter.AddXsl(@"Report\ExecutionEventReport.xsl");
            tableWriter.WriteStartElement("EventLog");
            foreach (EventLogEntry logEntry in infraLogEntries)
            {
                tableWriter.WriteStartElement("Event");

                tableWriter.WriteAttributeString("Source", logEntry.Source);                
                tableWriter.WriteAttributeString("Time", logEntry.TimeGenerated.ToString(CultureInfo.InvariantCulture));                
                tableWriter.WriteAttributeString("EventType", logEntry.EntryType.ToString());
                tableWriter.WriteAttributeString("Message", logEntry.Message);                
                              
                tableWriter.WriteEndElement();
            }
            tableWriter.WriteEndElement();
        }
    }
}