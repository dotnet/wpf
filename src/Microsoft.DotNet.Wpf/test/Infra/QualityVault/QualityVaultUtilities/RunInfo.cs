// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;
using System.IO;
using System.Xml;
using System;

namespace Microsoft.Test
{
    /// <summary>
    /// Information Describing a Test Run.
    /// This class contains policy which is "Lab Aware".
    /// </summary>
    public class RunInfo
    {
        #region Public Methods
        /// <summary>
        /// Packages the lab specified run settings into the RunInfo type, for consumption by reporting tools.
        /// </summary>
        /// <returns></returns>
        public static RunInfo FromEnvironment()
        {
            RunInfo info=new RunInfo();
            info.Architecture = Environment.GetEnvironmentVariable("ArchType");
            info.Branch = Environment.GetEnvironmentVariable("Branch");
            info.Build = Environment.GetEnvironmentVariable("Build");
            info.Date = Environment.GetEnvironmentVariable("RunDate");
            info.Dpi = Environment.GetEnvironmentVariable("DPI");
            info.Id = Environment.GetEnvironmentVariable("RunID");
            info.IEVersion = Environment.GetEnvironmentVariable("IEversion");
            info.InstallerPath = Environment.GetEnvironmentVariable("BuildPath");
            info.IsAppVerify = Environment.GetEnvironmentVariable("AppVerify");
            info.IsCodeCoverage = Environment.GetEnvironmentVariable("CodeCoverage");
            info.Language = Environment.GetEnvironmentVariable("OSLang");
            info.Name = Environment.GetEnvironmentVariable("RunTitle");
            info.OS = Environment.GetEnvironmentVariable("OSPlatform");
            info.Priority = Environment.GetEnvironmentVariable("Priority");
            info.RunType = Environment.GetEnvironmentVariable("RunTypeCode");
            info.TestBinariesPath = Environment.GetEnvironmentVariable("BinaryLocation");
            info.Version = Environment.GetEnvironmentVariable("VersionsFilter");
            info.RunTypeCode = Environment.GetEnvironmentVariable("RunTypeCode");

            //We don't track this as a lab specified thing right now... For now, this hinges on the machine performing distribution.
//            info.IsMultiMonitor = ParseBool(Environment.GetEnvironmentVariable("IsMultiMonitor"));
            return info;
        }

        /// <summary>
        /// Provides a rough report of Machine information, queried against OS info.
        /// Not guaranteed to be consistent with Lab specified properties. This is only produced on local runs.
        /// </summary>
        /// <returns></returns>
        public static RunInfo FromOS()
        {
            RunInfo info=new RunInfo();
            info.OS = Environment.OSVersion.ToString();
            info.Architecture = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");
            return info;
        }

        /// <summary>
        /// Loads RunInfo from from file.
        /// </summary>
        /// <param name="serializationDirectory"></param>
        /// <returns></returns>
        public static RunInfo Load(DirectoryInfo serializationDirectory)
        {                     
            RunInfo runinfo = null;
            FileInfo filePath=null;
            try
            {
                filePath = new FileInfo(Path.Combine(serializationDirectory.FullName, serializationFileName));
                using (XmlTextReader textReader = new XmlTextReader(filePath.OpenRead()))
                {
                    runinfo = (RunInfo)ObjectSerializer.Deserialize(textReader, typeof(RunInfo), null);
                }
            }
            catch (Exception e)
            {
                throw new IOException("Failed to Deserialize: " + filePath.FullName, e);
            }
            return runinfo;
        }

        /// <summary>
        /// Saves RunInfo to disk.
        /// </summary>
        /// <param name="serializationDirectory"></param>
        public void Save(DirectoryInfo serializationDirectory)
        {            
            if (!serializationDirectory.Exists)
            {
                serializationDirectory.Create();
            }
            FileInfo resultsFileInfo = new FileInfo(Path.Combine(serializationDirectory.FullName, serializationFileName));
            using (XmlTextWriter textWriter = new XmlTextWriter(resultsFileInfo.Open(FileMode.Create, FileAccess.Write), System.Text.Encoding.UTF8))
            {
                textWriter.Formatting = Formatting.Indented;
                ObjectSerializer.Serialize(textWriter, this);
            }            
        }

        #endregion

        private static string serializationFileName = "RunInfo.xml";

        #region Properties

        /// <summary>
        /// The ID of the run.
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// A Descriptive name of the run, can be used as a title.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The Date of the run.
        /// </summary>
        public string Date { get; set; }

        /// <summary>
        /// The path of the Product Installer.
        /// </summary>
        public string InstallerPath { get; set; }

        /// <summary>
        /// The path of the Test Binaries.
        /// </summary>
        public string TestBinariesPath { get; set; }

        /// <summary>
        /// The Build identifier 
        /// </summary>
        public string Build { get; set; }

        /// <summary>
        /// The Version control Branch used for the build.
        /// </summary>
        public string Branch { get; set; }

        /// <summary>
        /// The kind of Test Run.
        /// </summary>
        public string RunType { get; set; }

        /// <summary>
        /// ??? This is used in generating the lab mail headings.
        /// </summary>
        public string RunTypeCode { get; set; }

        /// <summary>
        /// The Priorities of tests being used in the run.
        /// </summary>
        public string Priority { get; set; }

        /// <summary>
        /// The Operating System used for the run.
        /// </summary>
        public string OS { get; set; }

        /// <summary>
        /// Specifies what CPU architectures are supported in this configuration.
        /// </summary>
        public string Architecture { get; set; }

        /// <summary>
        /// Specifies what OS Languge is being used in this configuration.
        /// </summary>
        public string Language { get; set; }

        /// <summary>
        /// Specifies what version of IE is being used in this configuration.
        /// </summary>
        public string IEVersion { get; set; }

        /// <summary>
        /// Specifies what DPI the run is operating at.
        /// </summary>
        public string Dpi { get; set; }

        /// <summary>
        /// Specifies what WPF Versions the run is targeted against.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Specifies if the run is using AppVerifier.
        /// </summary>
        public string IsAppVerify { get; set; }

        /// <summary>
        /// Is this a Code coverage run?
        /// </summary>
        public string IsCodeCoverage { get; set; }

        /// <summary>
        /// Specifies if the configuration requires Multi-Monitor support.
        /// </summary>
        public string IsMultiMonitor { get; set; }

        #endregion
    }
}