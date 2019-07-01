// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Test.Loaders;
using Microsoft.Test.Logging;
using Microsoft.Test.Diagnostics;

namespace Microsoft.Test.Loaders.Steps 
{

    /// <summary>
    /// Creates and Closes a FileHost for use in AppMonitorConfig files
    /// </summary>
    
    public class FileHostStep : LoaderStep
    {
        #region Public Members

        /// <summary>
        /// If true : Creates its fileHost using a Random UTF8 string (Selects randomly from several UTF character sets, all should work)
        /// </summary>
        public bool UseUTF8Path = false;

        /// <summary>
        /// If true, will upload files to the server relative to the subdirectories the files are in
        /// compared to the directory AppMonitor is running in.  Default: False
        /// </summary>
        public bool PreserveDirectoryStructure = false;

        /// <summary>
        /// Gets or sets an array of SupportFiles that should be remotely hosted
        /// </summary>
        public SupportFile[] SupportFiles = new SupportFile[0];

        /// <summary>
        /// Gets or sets the FileHost instance of the FileHostStep.
        /// </summary>
        public FileHost fileHost;

        /// <summary>
        /// Gets or sets string representing folder to copy to.  Normally will copy to random folder.
        /// </summary>
        public string UserDefinedDirectory = null;

        /// <summary>
        ///  Whether we should use the external server (specified in FTPTransferHelper) for uploading / deploying the app
        /// </summary>
        public bool UsingExternalServer = false;

        #endregion

        #region Step Implementation
        /// <summary>
        /// Creates a FileHost with the Name specified by the Name property
        /// </summary>
        /// <returns>true</returns>
        protected override bool BeginStep() 
        {
            if (UseUTF8Path)
            {
                if (UserDefinedDirectory != null)
                {
                    GlobalLog.LogEvidence("WARNING: UserDefinedDirectory set as well as UseUTF8Path.  Ignoring " + UserDefinedDirectory + " for randomly generated UTF-8 path");
                }
                UserDefinedDirectory = GenerateRandomUTF8FileName();
                GlobalLog.LogEvidence("Using UTF-8 folder name in file path: " + UserDefinedDirectory);
            }

            // Instantiate FileHost and upload support files
            fileHost = new FileHost(UserDefinedDirectory, UsingExternalServer);

            fileHost.PreserveDirectoryStructure = PreserveDirectoryStructure;

            foreach (SupportFile suppFile in SupportFiles)
            {
                if (suppFile.IncludeDependencies && !string.IsNullOrEmpty(suppFile.TargetDirectory))
                {
                    GlobalLog.LogEvidence("TargetDirectory with IncludeDependencies not yet implemented");
                    throw new NotImplementedException("TargetDirectory with IncludeDependencies not yet supported");
                }

                if (suppFile.CustomTestScratchServerPath == null)
                {
                    if (suppFile.IncludeDependencies)
                        fileHost.UploadFileWithDependencies(suppFile.Name);
                    else
                    {
                        fileHost.UploadFile(suppFile.Name, suppFile.TargetDirectory);
                    }
                }
                else
                {
                    fileHost.UploadFileNonDefaultServer(suppFile.Name, suppFile.CustomTestScratchServerPath);
                }
            }
            return true;
        }

        private string GenerateRandomUTF8FileName()
        {
            string UTF8Path = "РоссийскаяКодировка";

            // Vista or later... we can use JPN, RUS, KOR, or CHN text.  Pick one randomly to exercise string handling
            if (Environment.OSVersion.Version.Major >= 6)
            {
                string[] UTFNames = new string[] { "日本の符号化", "РоссийскаяКодировка", "한국어", "中国夹" };
                Random rand = new Random(DateTime.Now.Millisecond);
                UTF8Path = UTFNames[rand.Next(UTFNames.Length)];
            }    
            return DateTime.Now.Ticks.ToString() + UTF8Path;
        }

        /// <summary>
        /// Closes the TestLog
        /// </summary>
        /// <returns>true</returns>
        protected override bool EndStep() 
        {
            fileHost.Close();
            return true;
        }

        #endregion
    }
}