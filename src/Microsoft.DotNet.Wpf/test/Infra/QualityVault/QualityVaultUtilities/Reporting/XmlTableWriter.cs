// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml;

namespace Microsoft.Test.Reporting
{
    /// <summary>
    /// Covers boilerplate aspects for creating XML tables consumable in excel
    /// </summary>    
    internal class XmlTableWriter : XmlTextWriter, IDisposable
    {
        public XmlTableWriter(string path)
            : base(path, System.Text.Encoding.UTF8)
        {
            Formatting = Formatting.Indented;
            // Marking calls to base class explicitly to appease code analysis. Issue is that base may otherwise not be initialized at time of call.
            WriteStartDocument();

            //This allows for automatic Excel based loading of reports- Handy for reports with no Layout.
            //base.WriteProcessingInstruction("mso-application", "progid='Excel.Sheet'");            
        }

        void IDisposable.Dispose()
        {            
            WriteEndDocument();
            Close();
            base.Dispose(true);
            GC.SuppressFinalize(this);
        }

        internal void AddXsl(string p)
        {
            WriteProcessingInstruction("xml-stylesheet", "type='text/xsl' href='" + p + "'");
        }

        internal void WriteKeyValuePair(string key, string value)
        {
            WriteStartElement("KeyValuePair");
            WriteAttributeString("Key", key);
            WriteAttributeString("Value", value);
            WriteEndElement();
        }
    }
}