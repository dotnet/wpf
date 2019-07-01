// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml;
using System.IO;

namespace Microsoft.Test.LeakDetection
{
    /// <summary>
    /// A collection of MemorySnapshots that can be serialized to an XML file.
    /// </summary>
    /// 
    /// <example>
    /// The following example demonstrates taking multiple memory snapshots of Notepad 
    /// and saving them on disk for later analysis.
    /// <code>
    /// MemorySnapshotCollection c = new MemorySnapshotCollection();     
    /// 
    /// Process p = Process.Start("notepad.exe");
    /// p.WaitForInputIdle(5000);
    /// MemorySnapshot s1 = MemorySnapshot.FromProcess(p.Id);
    /// c.Add(s1);
    /// 
    /// // Perform operations that may cause a leak...
    /// 
    /// MemorySnapshot s2 = MemorySnapshot.FromProcess(p.Id);
    /// c.Add(s2);
    /// 
    /// c.ToFile(@"MemorySnapshots.xml");
    /// 
    /// p.CloseMainWindow();
    /// p.Close();
    /// </code>
    /// </example>
    /// 
    /// <example>
    /// A MemorySnapshotCollection can also be loaded from a XML file.
    /// <code>
    /// MemorySnapshotCollection c = MemorySnapshotCollection.FromFile(@"MemorySnapshots.xml");
    /// </code>
    /// </example>
    public class MemorySnapshotCollection : Collection<MemorySnapshot>
    {   
        /// <summary>
        /// Creates a MemorySnapshotCollection instance from data in the specified file.
        /// </summary>
        /// <param name="filePath">The path to the MemorySnapshotCollection file.</param>
        /// <returns>A MemorySnapshotCollection instance, containing memory information recorded in the specified file.</returns>
        public static MemorySnapshotCollection FromFile(string filePath)
        {
            MemorySnapshotCollection msColllection = new MemorySnapshotCollection();

            XmlDocument xmlDoc = new XmlDocument();
            using (Stream s = new FileInfo(filePath).OpenRead())
            {
                try
                {
                    xmlDoc.Load(s);
                }
                catch (XmlException)
                {
                    throw new XmlException("MemorySnapshotCollection file \"" + filePath + "\" could not be loaded.");
                }
            }

            XmlNodeList msNodeList = xmlDoc.DocumentElement.SelectNodes("MemorySnapshot");
            foreach (XmlNode msNode in msNodeList)
            {
                // Desrialize node.
                MemorySnapshot ms = MemorySnapshot.Deserialize(msNode);

                // Add to collection.
                msColllection.Add(ms);

            }

            return msColllection;
        }

        /// <summary>
        /// Writes the current MemorySnapshotCollection to a file.
        /// </summary>
        /// <param name="filePath">The path to the output file.</param>
        public void ToFile(string filePath)
        {
            if (String.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException("MemorySnapshot.ToFile(): the specified file path \"" + filePath + "\" is null or empty.");
            }

            XmlDocument xmlDoc = new XmlDocument();
            XmlNode rootNode = xmlDoc.CreateElement("MemorySnapshotCollection");

            foreach (MemorySnapshot ms in this.Items)
            {
                // Call serializer on MemorySnapshot.
                XmlNode msNode = ms.Serialize(xmlDoc);

                // Append to MemorySnapshotCollection.
                rootNode.AppendChild(msNode);
            }            

            xmlDoc.AppendChild(rootNode);
            xmlDoc.Save(filePath);            
        }        
    }
}