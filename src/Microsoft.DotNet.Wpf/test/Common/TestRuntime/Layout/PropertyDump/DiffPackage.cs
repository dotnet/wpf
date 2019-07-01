// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Xml;
using System.Collections;
using System.Globalization;
using Microsoft.Test.Compression;
using Microsoft.Test.Logging;
using System.Text;
using System.Reflection;

namespace Microsoft.Test.Layout.PropertyDump
{
    /// <summary>s
    /// A Class for handling a DiffPackage for analysis
    /// </summary>
    public class DiffPackage : IDisposable
    {
        #region Constants
     
        private const string defaultExtension   = ".fdpk";
        private const string defaultCompareTool = "windiff";
        private const string masterKey          = "MASTER";
        private const string resultKey          = "RESULT";
        private const string additionalKey      = "ADDITIONAL";
        private const string infoKey            = "INFO";
        private const string infoXmlFile        = "Info.xml";
        private const string testInfoFile       = "TestInfo.xml";
        private const string testInfoKey        = "TESTINFO";

        #endregion Constants

        #region Member variables
        
        /// <summary>
        /// The archiver to archive all information for the package
        /// </summary>
        private Archiver archiver = null;

        #endregion Member variables

        #region Properties
        
        /// <summary>
        /// The path to the local copy of the master
        /// </summary>
        public string MasterFile
        {
            get { return masterFile; }
            set { masterFile = value; }
        }
        private string masterFile = null;
        
        /// <summary>
        /// The path to the local copy of the result
        /// </summary>
        public string ResultFile
        {
            get { return resultFile; }
            set { resultFile = value; }
        }
        private string resultFile = null;

        /// <summary>
        /// The location where the master was located at runtime
        /// </summary>
        /// <remarks>Defaults to the full path to the given MasterFile</remarks>
        public string MasterLocation
        {
            get { return masterLocation; }
            set
            {
                if (value == null || value == string.Empty)
                {
                    GlobalLog.LogEvidence(new Exception("The MasterLocation MUST contain the master filename"));
                }
                else
                {
                    masterLocation = value;
                }
            }
        }
        private string masterLocation = null;

        /// <summary>
        /// The path to the master in SD (minus the depot root)
        /// </summary>
        public string MasterSDPath
        {
            get { return masterSDPath; }
            set { masterSDPath = value; }
        }
        private string masterSDPath = null;
        
        /// <summary>
        /// The tool to use to compare master and result
        /// </summary>
        public string CompareType
        {
            get { return compareType; }
            set { compareType = value; }
        }
        private string compareType = defaultCompareTool;

        /// <summary>
        /// Test Case info used in updating masters.
        /// </summary>
        public string TestInfo
        {
            get { return testInfo; }
            set { testInfo = value; }
        }
        private string testInfo = null;

        #endregion Properties

        /// <summary>
        /// Empty Constructor
        /// Block Instantiation of DiffPackage
        /// </summary>
        private DiffPackage()
        { }

        /// <summary>
        /// Construct a DiffPackage from the given archive
        /// </summary>
        /// <param name="givenArchiver">The archive to extract</param>
        private DiffPackage(Archiver givenArchiver)
            : this()
        {
            this.archiver = givenArchiver;
            
            //Get Package files
            MasterFile = archiver.GetFileLocation(masterKey);
            ResultFile = archiver.GetFileLocation(resultKey);
            TestInfo = archiver.GetFileLocation(testInfoKey);
            string infoFile = archiver.GetFileLocation(infoKey);

            //Parse the info file for master location & sd path
            ParseInfoXmlFile(infoFile);
        }

        /// <summary>
        /// Constructs a DiffPackage with the given files
        /// </summary>
        /// <param name="resultFilename">The filename (including path) of the result file</param>
        /// <param name="masterFilename">The filename (including path) of the master file</param>
        /// <remarks>You MUST call Save to write the package to disk</remarks>
        public DiffPackage(string resultFilename, string masterFilename)
            : this(resultFilename, masterFilename, null)
        { }

        /// <summary>
        /// Constructs a DiffPackage with the given files and path
        /// </summary>
        /// <param name="resultFilename">The filename (including path) of the result file</param>
        /// <param name="masterFilename">The filename (including path) of the master file</param>
        /// <param name="sdPathToMaster">The path to the master in SD</param>
        /// <remarks>You MUST call Save to write the package to disk</remarks>
        public DiffPackage(string resultFilename, string masterFilename, string sdPathToMaster)
        {
            if (resultFilename != null||resultFilename != string.Empty)
            {
                ResultFile = resultFilename;
            }

            if (masterFilename != null || masterFile != string.Empty)
            {
                MasterFile = masterFilename;
            }

            if (sdPathToMaster != null ||sdPathToMaster != string.Empty)
            {
                MasterSDPath = sdPathToMaster;
            }
        }

        #region Static Methods

        /// <summary>
        /// Load an existing DiffPackage
        /// </summary>
        /// <param name="packageName">The name of the package to be loaded</param>
        /// <returns>The DiffPackage that was loaded</returns>
        static public DiffPackage Load(string packageName)
        {
            Archiver loadedArchiver = Archiver.Load(packageName);
            return new DiffPackage(loadedArchiver);
        }

        #endregion Static Methods

        #region Public Methods

        ///// <summary>
        ///// Add additional files to the package
        ///// </summary>
        ///// <param name="filename">The filename (including path) of the file to add</param>
        //public void AddAdditionalFile(string filename)
        //{
        //    if (additionalFiles == null)
        //    {
        //        additionalFiles = new ArrayList();
        //    }
        //    additionalFiles.Add(Path.GetFullPath(filename));
        //}

        ///// <summary>
        ///// Gets the list of additional files in the package
        ///// </summary>
        ///// <returns>The list of full paths to the additional files</returns>
        //public string[] GetAdditionalFiles()
        //{
        //    if (additionalFiles != null)
        //    {
        //        return (string[])additionalFiles.ToArray(typeof(string));
        //    }
        //    return null;
        //}

        /// <summary>
        /// Save the package on disk
        /// </summary>
        /// <param name="packageFilename"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        public string Save(string packageFilename, Arguments arguments)
        {
            // Set the package to the given filename
            string package = arguments.Name;

            if (Path.GetExtension(package).Length == 0)
            {
                package += defaultExtension;
            }

            if (resultFile == null || !File.Exists(resultFile))
            {
                //throw new Exception("The package MUST contain a result file before being saved");
                GlobalLog.LogEvidence(new Exception("The package MUST contain a result file before being saved"));
            }

            // Fully expand the archiver name to ensure it is saved in the requested place
            // Create archiver
            archiver = Archiver.Create(Path.GetFullPath(package), true);

            // Add Master File
            if (masterFile != null)
            {
                AddFileFromPath(MasterFile, masterKey);
            }

            // Add the Result File
            AddResultFileFromPath();

            // Add info xml File
            CreateInfoXmlFile(arguments);

            if (File.Exists(infoXmlFile))
            {
                archiver.AddFile(infoXmlFile, infoKey, true);
            }

            //creates test info file for future use in master update app.
            CreateTestInfoXml(arguments);

            if (File.Exists(TestInfo))
            {
                //AddFileFromPath(TestInfoFile, "TestInfoFile");
                archiver.AddFile(TestInfo, testInfoKey, true);
            }

            // Generate package
            archiver.GenerateArchive();

            // Return the full path to the archive
            return Path.GetFullPath(package);
        }

        /// <summary>
        /// Save the package on disk
        /// </summary>
        /// <param name="arguments"></param>
        /// <returns></returns>
        public string Save(Arguments arguments)
        {
            return Save(null, arguments);
        }

        #endregion Public Methods

        #region Private Methods
        
        /// <summary>
        /// Write the additional information to the info file
        /// </summary>
        private void CreateInfoXmlFile(Arguments arguments)
        {
            XmlTextWriter textWriter = null;
            try
            {
                //Create the additional info xml document
                textWriter = new XmlTextWriter(infoXmlFile, System.Text.UTF8Encoding.UTF8);
                textWriter.Formatting = Formatting.Indented;
                textWriter.WriteStartDocument();
                textWriter.WriteStartElement("info");

                //If the master location is not set then use the path to the master that was given
                if (MasterLocation == null && masterFile != null)
                {
                    MasterLocation = arguments.ComparePath;
                }

                if (MasterLocation != null)
                {
                    //Write the master location to the file if it exists
                    textWriter.WriteElementString("master", masterLocation);
                }
                if (MasterSDPath != null)
                {
                    //Write the master sd path to the file if it exists
                    textWriter.WriteElementString("sdpath", masterSDPath);
                }
                if (CompareType != null)
                {
                    //Write the compare tool to the file if it exists
                    textWriter.WriteElementString("compare", compareType);
                }
                textWriter.WriteEndElement();
                textWriter.WriteEndDocument();
            }
            catch (Exception e)
            {
                //throw e;
                GlobalLog.LogEvidence(e);
            }
            finally
            {
                if (textWriter != null)
                {
                    textWriter.Flush();
                    textWriter.Close();
                }
            }
        }

        /// <summary>
        /// Fills the member variables from the additional information file
        /// </summary>
        /// <param name="infoFile">The full path to the additional information file on the disk</param>
        private void ParseInfoXmlFile(string infoFile)
        {
            XmlDocument xmlDoc = null;
            try
            {
                //Load the information document
                xmlDoc = new XmlDocument();
                xmlDoc.Load(infoFile);

                //Get the master location if it is contained in the Information file
                XmlNode nodeMasterLocation = xmlDoc.DocumentElement.SelectSingleNode("master");
                if ((nodeMasterLocation != null) && (nodeMasterLocation.InnerText.Length != 0))
                {
                    MasterLocation = nodeMasterLocation.InnerText;
                }

                //Get the master sd path if it is contained in the Information file
                XmlNode nodeMasterSDPath = xmlDoc.DocumentElement.SelectSingleNode("sdpath");
                if ((nodeMasterSDPath != null) && (nodeMasterSDPath.InnerText.Length != 0))
                {
                    masterSDPath = nodeMasterSDPath.InnerText;
                }

                //Get the compare tool if it is contained in the Information file
                XmlNode nodeCompare = xmlDoc.DocumentElement.SelectSingleNode("compare");
                if ((nodeCompare != null) && (nodeCompare.InnerText.Length != 0))
                {
                    compareType = nodeCompare.InnerText;
                }
            }
            catch (Exception e)
            {
                GlobalLog.LogEvidence(e);
                //throw e;
            }
        }

        /// <summary>
        /// Adds the result file to the archive after copying to the current directory
        /// </summary>
        /// <remarks>May rename file if it collides with Master</remarks>
        private void AddResultFileFromPath()
        {
            //Check if the given file has the same filename as the master file
            if (masterFile != null && Path.GetFileName(masterFile).ToLower(CultureInfo.CurrentCulture) == Path.GetFileName(resultFile).ToLower(CultureInfo.CurrentCulture))
            {
                //Create the temporary location for the copy of the given file
                string newFile = Path.GetFileNameWithoutExtension(resultFile) + resultKey + Path.GetExtension(resultFile);

                //Copy the existing file to the current directory
                File.Copy(resultFile, newFile, true);

                //Add the copy to the file
                archiver.AddFile(newFile, resultKey, true);
            }
            else
            {
                AddFileFromPath(resultFile, resultKey);
            }
        }

        /// <summary>
        /// Adds the given file to the archive after copying to the current directory
        /// </summary>
        /// <param name="fileName">The full path to the file on the disk</param>
        /// <param name="key">The key to save the file under in the archive</param>
        private void AddFileFromPath(string fileName, string key)
        {
            //Check whether the file exists before trying to add it
            if (File.Exists(fileName))
            {
                //Create the temporary location for the copy of the given file
                string newFile = Path.GetFileName(fileName);

                //Check if the given file is already in the current directory
                if (newFile.ToLower(CultureInfo.CurrentCulture) != fileName.ToLower(CultureInfo.CurrentCulture) &&
                    Path.GetFullPath(newFile).ToLower(CultureInfo.CurrentCulture) != Path.GetFullPath(fileName).ToLower(CultureInfo.CurrentCulture))
                {
                    //Copy the existing file to the current directory
                    File.Copy(fileName, newFile, true);

                    //Add the copy to the file
                    archiver.AddFile(newFile, key, true);
                }
                else
                {
                    //Add the copy to the file
                    archiver.AddFile(newFile, key, false);
                }
            }
        }

        private void CreateTestInfoXml(Arguments arguments)
        {
            TestInfo = string.Format("TestInfo.xml");
            XmlTextWriter writer = null;
            try
            {
                //Create the additional info xml document
                writer = new XmlTextWriter(testInfoFile, UTF8Encoding.UTF8);
                writer.Formatting = Formatting.Indented;
                writer.WriteStartDocument();
                writer.WriteStartElement("TESTCASE");

                foreach (PropertyInfo pi in arguments.GetType().GetProperties())
                {
                    writer.WriteElementString(pi.Name, pi.GetValue(arguments, null).ToString());
                }

                writer.WriteEndElement();
                writer.WriteEndDocument();
            }
            catch (Exception e)
            {
                //throw e;
                GlobalLog.LogEvidence(e);
            }
            finally
            {
                if (writer != null)
                {
                    writer.Flush();
                    writer.Close();
                }
            }
        
        }

        #endregion Private Methods

        /// <summary>
        /// Release all allocated/locked resources
        /// </summary>
        public void Dispose()
        {
            archiver = null;
        }
    }
}