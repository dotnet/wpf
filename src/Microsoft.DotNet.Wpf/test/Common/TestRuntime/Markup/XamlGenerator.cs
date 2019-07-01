// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Reflection;
using System.IO;
using System.Collections;
using System.Diagnostics;
using System.Text;

namespace Microsoft.Test.Markup
{
    /// <summary>
    /// Creates xaml files or streams based on a xaml schema and parameters 
    /// for the number of allowed children, attributes, and tree depth.
    /// </summary>
    public class XamlGenerator : XmlGenerator
    {
        ///// <summary>
        ///// Loads xaml.xsd from assembly resource.
        ///// </summary>
        //public XamlGenerator()
        //{
        //    lock (_syncObject)
        //    {
        //        // Load xaml schema from current directory.
        //        Stream xsdStream = File.OpenRead("xaml.xsd");

        //        // Pass stream to base contructor.
        //        _Initialize(xsdStream, new Stream[0]);

        //        xsdStream.Close();
        //    }
        //}

        ///// <summary>
        ///// Loads xaml.xsd from assembly resource and automatically imports add-on schemas.
        ///// </summary>
        ///// <param name="importSchemas">Add-on schemas to merge with the main xaml schema.</param>
        //public XamlGenerator(Stream[] importSchemas)
        //{
        //    lock (_syncObject)
        //    {
        //        // Load xaml schema from current directory.
        //        Stream xsdStream = File.OpenRead("xaml.xsd");

        //        _Initialize(xsdStream, importSchemas);
        //    }
        //}

        /// <summary>
        /// Accepts schema filename(s) that will direct the kinds of xamls to create.  The main
        /// schema specified replaces default main schema xaml.xsd.
        /// </summary>
        /// <param name="mainSchema">The main schema file to use.  Replaces the default Avalon xaml schema.</param>
        /// <param name="importSchemas">Add-on schemas to merge with the main schema.</param>
        public XamlGenerator(Stream mainSchema, params Stream[] importSchemas)
            : base(mainSchema, importSchemas)
        {
        }

        /// <summary>
        /// Loads xaml.xsd from assembly resource and automatically imports add-on schemas.
        /// </summary>
        /// <param name="importSchemas">Add-on schemas to merge with the main xaml schema.</param>
        public XamlGenerator(string[] importSchemas)
        {
            lock (_syncObject)
            {
                // Load xaml schema from assembly resource.
                Stream mainStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(_xamlSchemaName);

                Stream[] importStreams = new Stream[importSchemas.Length];

                try
                {
                    // Get streams for import schemas.
                    for (int i = 0; i < importStreams.Length; i++)
                    {
                        string filePath = importSchemas[i];

                        if (!File.Exists(filePath))
                        {
                            throw new ArgumentException("Add-on schema file does not exist:" + filePath, "importSchemas");
                        }

                        FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

                        importStreams[i] = fileStream;
                    }

                    // Merge and compile schemas.
                    _Initialize(mainStream, importStreams);
                }
                finally
                {
                    // Close streams if necessary

                    if (mainStream != null)
                        mainStream.Close();

                    for (int i = 0; i < importStreams.Length; i++)
                    {
                        Stream importStream = importStreams[i];

                        if (importStream != null)
                            importStreams[i].Close();
                    }
                }
            }
        }



        /// <summary>
        /// Loads xaml.xsd from assembly resource and automatically imports add-on schemas.
        /// </summary>
        /// <param name="mainSchema">The main schema file to use.</param>
        /// <param name="importSchemas">Add-on schemas to merge with the main xaml schema.</param>
        public XamlGenerator(string mainSchema, params string[] importSchemas)
            : base(mainSchema, importSchemas)
        {
        }

        /// <summary>
        /// Adds a &lt;?Mapping?&gt; processing instruction (PI) to a list.  Generated xmls will contain the Mapping PIs.
        /// </summary>
        /// <param name="xmlNamespace"></param>
        /// <param name="clrNamespace"></param>
        /// <param name="assembly"></param>
        /// <remarks>
        /// Calling with xmlNamespace="custom", clrNamespace="Windows.Test.Custom", and assembly="CustomControls" would
        /// cause the following line to be included in every generated xml:
        /// &lt;?Mapping XmlNamespace="custom" ClrNamespace="Windows.Test.Custom" Assembly="CustomControls" ?&gt;
        /// </remarks>
        public void AddMappingPI(string xmlNamespace, string clrNamespace, string assembly)
        {
            MappingPIComponent mapppingPIComponenet = new MappingPIComponent();

            mapppingPIComponenet.XmlNamespace = xmlNamespace;
            mapppingPIComponenet.ClrNamespace = clrNamespace;
            mapppingPIComponenet.Assembly = assembly;

            _mappingPIs.Add(mapppingPIComponenet);
        }

        /// <summary>
        /// Inserts &lt;?Mapping?&gt; processing instructions (PIs) at the beginning of generated xml.
        /// </summary>
        /// <param name="xmlDocument"></param>
        protected override void CreateXmlCore(XmlDocument xmlDocument)
        {
            // Create mappingPI
            _CreateMappingPIs(xmlDocument);

            // Insert newline
            XmlWhitespace ws = xmlDocument.CreateWhitespace("\r\n");
            xmlDocument.AppendChild(ws);
        }

        /// <summary>
        /// Writes Mapping PIs.
        /// </summary>
        private void _CreateMappingPIs(XmlDocument xmlDocument)
        {
            for (int i = 0; i < _mappingPIs.Count; i++)
            {
                MappingPIComponent mappingPI = _mappingPIs[i] as MappingPIComponent;

                string mapping = "XmlNamespace=\""
                    + mappingPI.XmlNamespace
                    + "\" ClrNamespace=\""
                    + mappingPI.ClrNamespace
                    + "\" Assembly=\""
                    + mappingPI.Assembly
                    + "\"";

                XmlNode piNode = xmlDocument.CreateProcessingInstruction("Mapping", mapping);
                xmlDocument.AppendChild(piNode);
            }
        }

        /// <summary>
        /// Creates multiple xaml files.  The total number depends on
        /// the user parameters and the schema.
        /// </summary>
        /// <param name="maxDepth">The maximum depth of each xaml tree</param>
        /// <param name="maxAttributes">The maximum number of attribute for each node</param>
        /// <param name="maxChildren">The maximum number of children and complex syntax properties for each node</param>
        /// <param name="maxFiles">The maximum number of files to create.</param>
        /// <param name="outDirectory">The directory to save generated xaml files.</param>
        /// <param name="filePrefix">The prefix to put on every saved xaml filename.</param>
        public void CreateFiles(int maxDepth, int maxAttributes, int maxChildren, int maxFiles, string outDirectory, string filePrefix)
        {
            this.CreateFiles(maxDepth, maxAttributes, maxChildren, maxFiles, outDirectory, filePrefix, ".xaml");
        }

        // Sync object.
        static object _syncObject = new object();

        // xaml schema name.
        private string _xamlSchemaName = "xaml.xsd";

        // List of Mapping PIs to put at beginning of generated xmls.
        ArrayList _mappingPIs = new ArrayList();

        /// <summary>
        /// Default Avalon Namespace 
        /// </summary>
        public static string AvalonXmlns = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

        /// <summary>
        /// xmlns:x Namespace
        /// </summary>
        public static string AvalonXmlnsX = "http://schemas.microsoft.com/winfx/2006/xaml";
    }
}
