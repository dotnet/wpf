// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using Microsoft.Test.Security.Wrappers;

namespace Microsoft.Test.Utilities.VariationEngine
{
    /// <summary>
    /// Filename and FileExtension.
    /// </summary>
    internal struct FileData
    {
        /// <summary>
        /// File Name
        /// </summary>
        internal string FileName;

        /// <summary>
        /// File Extension.
        /// </summary>
        internal string FileExtension;

    }

    /// <summary>
    /// Summary description for XmlFileGenerator.
    /// </summary>
    public class XmlFileVariationGenerator : VariationGenerator
    {

        #region Member variables
        internal string generatedFile = null;
        private FileData definedfiledata;
        private FileData defaultfiledata;

        internal string _fileoutputdirectory = null;

        internal ArrayList elementstoberemoved = null;

        const string FileNameElement = "FileName";
        const string FileExtensionElement = "FileExtension";
        const string FileRetainFileNameElement = "RetainFileName";
        const string FileIsXmlDocumentElement = "IsXmlDocument";

        bool _retainfilename = false;
        bool _isxmldocument = true;

        #endregion

        #region Public Methods
        /// <summary>
        /// Public Constructor that calls Initialize
        /// </summary>
        public XmlFileVariationGenerator()
        {
            Initialize();
        }

        /// <summary>
        /// Dispose objects that are not required anymore.
        /// </summary>
        public override void Dispose() 
        {
            base.Dispose();
            elementstoberemoved = null;
        }

        /// <summary>
        /// Read additional file information.
        /// </summary>
        /// <param name="xmldatafile"></param>
        /// <returns></returns>
        public override bool Read(string xmldatafile)
        {
            defaultfiledata.FileName = PathSW.GetFileNameWithoutExtension(xmldatafile);
            defaultfiledata.FileExtension = PathSW.GetExtension(xmldatafile);

            return base.Read(xmldatafile);
        }

        /// <summary>
        /// Encapsulated method - Calls another method with scenario id and variationids as null.
        /// </summary>
        /// <param name="scenarioid"></param>
        /// <returns></returns>
        public bool GenerateVariationFile(string scenarioid)
        {
            return GenerateVariationFile(scenarioid, null);
        }

        /// <summary>
        /// Encapsulated Method - Takes 2 input params 
        /// Method that performs the actual file generation.
        /// Steps :
        ///		1. Get into context - by getting matching scenario by passed in scenarioid
        ///		2. Call base method ApplyScenarios based on variationids value.
        ///		3. If FileName and FileExtension elements were specified as variations get the new values.
        ///		4. Remove these elements from the document.
        ///		5. Generate file and store file name informations in a list.
        /// </summary>
        /// <param name="scenarioid"></param>
        /// <param name="variationids"></param>
        public bool GenerateVariationFile(string scenarioid, string[] variationids)
        {
            // Apply Scenarios to TemplateData document based on variationid's existing or not.
            if (variationids == null)
            {
                if (base.ApplyScenario(scenarioid) == false)
                {
                    //Console.WriteLine("Scenario {0} could not be applied", scenarioid);
                    return false;
                }
            }
            else
            {
                if (base.ApplyScenario(scenarioid, variationids) == false)
                {
                    //Console.WriteLine("Scenario {0} could not be applied with variation combinations.", scenarioid);
                    return false;
                }
            }

            // Final file name is based on scenario + variation string get the variationstring.
            string variationstring = null;

            if (variationids != null)
            {
                for (int j = 0; j < variationids.Length; j++)
                {
                    variationstring += variationids[j];
                }
            }
            else
            {
                variationstring = "_all";
            }

            //			// Go generate file
            GenerateFile(scenarioid, variationstring);

            return true;
        }

        #endregion

        #region Protected Methods
        /// <summary>
        /// If a FileName element or FileExtensionElement is found in the Scenario element
        /// process them.
        /// </summary>
        /// <param name="unrecognizednode"></param>
        /// <returns></returns>
        protected override bool ProcessUnRecognizedElements(XmlNode unrecognizednode)
        {
            if (unrecognizednode == null)
            {
                return false;
            }

            string szretval = null;
            switch (unrecognizednode.Name)
            {
                case FileNameElement:
                    if ( CommonHelper.IsSpecialValue(unrecognizednode.InnerText) )
                    {
                        szretval = CommonHelper.DeriveSpecialValues(unrecognizednode.InnerText, unrecognizednode);
                        if (String.IsNullOrEmpty(szretval) == false)
                        {
                            defaultfiledata.FileName = szretval;
                        }
                    }
                    
                    if ( String.IsNullOrEmpty(szretval) )
                    {
                        definedfiledata.FileName = unrecognizednode.InnerText;
                    }

                    return true;

                case FileExtensionElement:
                    definedfiledata.FileExtension = unrecognizednode.InnerText;
                    return true;

                case FileRetainFileNameElement:
                    if (Convert.ToBoolean(unrecognizednode.InnerText))
                    {
                        _retainfilename = true;
                    }
                    return true;
            }

            return base.ProcessUnRecognizedElements(unrecognizednode);
        }

        /// <summary>
        /// Generate the actual file.
        /// </summary>
        /// <param name="finalxmldocument"></param>
        [CLSCompliant(false)]
        protected virtual void GenerateFile(ref XmlDocumentSW finalxmldocument)
        {
            // Unautorized exception can occur
            // Now vardoc only contains relevant data.
            XmlNodeReaderSW xnr = new XmlNodeReaderSW((XmlNode)finalxmldocument.InnerObject);
            XmlTextWriterSW xtr = null;

            Console.Write("Generating {0} file .", generatedFile);
            System.Text.Encoding currentencoding = System.Text.Encoding.ASCII;

            if (CommonHelper.FileModified)
            {
                currentencoding = System.Text.Encoding.Unicode;
            }

            xtr = new XmlTextWriterSW(generatedFile, currentencoding);

            // Generate file
            if (xtr != null)
            {
                Console.Write(".");
                xtr.Flush();
                xtr.Formatting = Formatting.Indented;
                xtr.Indentation = 4;
                xtr.WriteNode((XmlNodeReader)xnr.InnerObject, true);
                xnr.Close();
                //				vardoc.GetElementsByTagName(Macros.TemplateDataElement)[0].WriteContentTo(xtr);
                xtr.Close();
                Console.WriteLine(". Done");
            }

        }

        /// <summary>
        /// Generate Text file
        /// </summary>
        /// <param name="filetext"></param>
        protected virtual void GenerateFile(string filetext)
        {
            Console.Write("Generating {0} text file .", generatedFile);

            System.Text.Encoding currentencoding = System.Text.Encoding.ASCII;

            if (CommonHelper.FileModified)
            {
                currentencoding = System.Text.Encoding.Unicode;
            }

            StreamWriterSW sw = new StreamWriterSW(generatedFile, false, currentencoding);
            sw.Flush();

            sw.WriteLine(filetext);
            sw.Close();                
        }

        /// <summary>
        /// Read <FileName/> and <FileExtension/> elements if specified in the Defaults section 
        /// in a Scenarios document.
        /// </summary>
        /// <param name="node"></param>
        protected override void ReadDefaults(XmlNode node)
        {
            string szretval = null;

            switch (node.Name)
            {
                case FileNameElement:
                    if (String.IsNullOrEmpty(node.InnerText) == false)
                    {
                        if (CommonHelper.IsSpecialValue(node.InnerText))
                        {
                            szretval = CommonHelper.DeriveSpecialValues(node.InnerText, node);
                            if (String.IsNullOrEmpty(szretval) == false)
                            {
                                defaultfiledata.FileName = szretval;
                            }
                        }

                        if (String.IsNullOrEmpty(szretval))
                        {
                            definedfiledata.FileName = node.InnerText;
                        }
                    }

                    break;

                case FileExtensionElement:
                    if (String.IsNullOrEmpty(node.InnerText) == false)
                    {
                        defaultfiledata.FileExtension = node.InnerText;
                    }

                    break;

                case FileRetainFileNameElement:
                    if (String.IsNullOrEmpty(node.InnerText))
                    {
                        _retainfilename = false;
                    }

                    _retainfilename = Convert.ToBoolean(node.InnerText);

                    break;

                case FileIsXmlDocumentElement:
                    if (String.IsNullOrEmpty(node.InnerText))
                    {
                        _isxmldocument = false;
                    }

                    _isxmldocument = Convert.ToBoolean(node.InnerText);
                    break;
            }

            base.ReadDefaults(node);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Encapsulated method - taking 3 input params
        /// Does the file generation. 
        /// First based on inputs decides the final file name for the file to be generated.
        /// </summary>
        /// <param name="scenarioid"></param>
        /// <param name="variationstring"></param>
        private void GenerateFile(string scenarioid, string variationstring)
        {
            // If no scenarioid is defined set to all.
            if (scenarioid == null)
            {
                scenarioid = "_all";
            }

            // If newfiledata is null make a bunch of checks to come up with final name and fileextension.
            FileData tempfiledata = definedfiledata;

            if (String.IsNullOrEmpty(tempfiledata.FileName))
            {
                // Use default filename (defined in Attribute or element in TemplateData). Refer GetTemplateData method.
                tempfiledata.FileName = defaultfiledata.FileName;
            }

            if (String.IsNullOrEmpty(tempfiledata.FileExtension))
            {
                // Use default fileextension (defined in Attribute or element in TemplateData). Refer GetTemplateData method.
                tempfiledata.FileExtension = defaultfiledata.FileExtension;
            }

            // Generate XmlDocument with defaultdata document innerxml.
            if (base.canvasdoc == null)
            {
                UtilsLogger.LogError = "Variation document is null";
                return;
            }

            if (base.canvasdoc.GetElementsByTagName(Constants.TemplateDataElement) == null)
            {
                UtilsLogger.LogError = "Variation document did not contain TemplateData element";
                return;
            }

            if (base.canvasdoc.GetElementsByTagName(Constants.TemplateDataElement).Count > 1)
            {
                UtilsLogger.LogError = "Variation document contained more than one TemplateData element";
                return;
            }

            // Generated file name.
            if (_retainfilename == false)
            {
                if (variationstring != null)
                {
                    generatedFile = tempfiledata.FileName + "_Sc" + scenarioid + "_Var" + variationstring + tempfiledata.FileExtension;
                }
                else
                {
                    generatedFile = tempfiledata.FileName + "_Sc" + scenarioid + "_Var" + "all" + tempfiledata.FileExtension;
                }
            }
            else
            {
                generatedFile = tempfiledata.FileName + tempfiledata.FileExtension;
            }

            if (String.IsNullOrEmpty(_fileoutputdirectory) == false)
            {
                if (DirectorySW.Exists(_fileoutputdirectory))
                {
                    generatedFile = _fileoutputdirectory + PathSW.DirectorySeparatorChar + generatedFile;
                }
            }

            if (generatedFile.Contains(PathSW.DirectorySeparatorChar.ToString()))
            {
                string directoryname = PathSW.GetDirectoryName(generatedFile);
                if (string.IsNullOrEmpty(directoryname) == false)
                {
                    if (DirectorySW.Exists(directoryname) == false)
                    {
                        if (DirectorySW.Exists(directoryname) == false)
                        {
                            DirectorySW.CreateDirectory(directoryname);
                        }
                    }
                }
            }

            if (_isxmldocument)
            {
                XmlDocumentSW tempdoc = new XmlDocumentSW();
                tempdoc.LoadXml(base.canvasdoc.DocumentElement.InnerXml);

                GenerateFile(ref tempdoc);

                tempdoc = null;
            }
            else
            {
                GenerateFile(base.canvasdoc.DocumentElement.InnerText);
            }            
        }

        /// <summary>
        /// Initializer for all constructors
        /// Initializes the elements to be removed arraylist and initializes the
        /// generated files list.
        /// </summary>
        private void Initialize()
        {
            CommonHelper.FileModified = false;
            elementstoberemoved = new ArrayList();
        }

        #endregion

        #region Public Properties
        /// <summary>
        /// Output directory for the file to be generated in.
        /// </summary>
        /// <value></value>
        public string OutputDirectory
        {
            set
            {
                _fileoutputdirectory = value;
            }
        }

        #endregion

    }
}
