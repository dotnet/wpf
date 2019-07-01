// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Test.Execution;
using System.IO;
using System;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.Win32;
namespace Microsoft.Test
{
    /// <summary>
    /// Contains mechanisms for weaving use of Code Coverage tooling within QV workflows.
    /// </summary>
    public class CodeCoverageUtilities
    {

        #region Private Data

        private static string coverageToolPath { get { return Path.Combine(CodeCoveragePath, "covercmd.exe"); } }
        private static string coverageDataToolPath { get { return Path.Combine(CodeCoveragePath, "covdata.exe"); } }


        private static readonly string magellanSource = @"\\ddcov\Maginstall\Current\SilentInstall\MagAutoInstall.vbs";
        private static readonly string installCommand = @"/Coverage Full /Injection Full /DestDir " + CodeCoveragePath;
        private static readonly string unInstallCommand = "/Uninstall on";

        private const string codeCoveragePathRegistryKey = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Microsoft Magellan Toolset\Sleuth";


        private static string codeCoveragePath;
        private static string CodeCoveragePath
        {
            get
            {
                if (string.IsNullOrEmpty(codeCoveragePath))
                {
                    try
                    {
                        codeCoveragePath = Registry.GetValue(codeCoveragePathRegistryKey, "", null) as string;
                    }
                    catch (Exception e)
                    {
                        throw new InvalidOperationException("Magellan Toolset Installation path could not be retrieved from Registry. Is it properly installed?", e);
                    }
                }
                return codeCoveragePath;
            }
        }

        #endregion

        #region Execution Commands

        internal static void Install()
        {
            ProcessUtilities.Run("cmd", "/K " + magellanSource + " " + installCommand);
        }


        internal static void Uninstall()
        {
            ProcessUtilities.Run("cmd", "/K " + magellanSource + " " + unInstallCommand);
        }


        /// <summary>
        /// Start tracing with Magellan. This can be done at scope of entire run or individual invocations
        /// </summary>        
        internal static void BeginTrace()
        {
            ExecutionEventLog.RecordStatus("Starting Code Coverage session.");
            ProcessUtilities.Run(coverageToolPath, "/Close /session ALL");
            ProcessUtilities.Run(coverageToolPath, "/Reset /session ALL");
        }

        /// <summary>
        /// End Tracing.        
        /// Assembly specific trace files will be generated.
        /// </summary>
        internal static void EndTrace(DirectoryInfo coverageLogDirectory, bool retainResult, string traceName)
        {
            if (retainResult)
            {
                ExecutionEventLog.RecordStatus("Retaining Coverage data for passing test as trace: " + traceName);
                if (!coverageLogDirectory.Exists)
                {
                    coverageLogDirectory.Create();
                }
                // List harvested components and then save to disk
                ProcessUtilities.Run(coverageToolPath, "/List /session ALL");
                ProcessUtilities.Run(coverageToolPath, "/SetPath \"" + coverageLogDirectory.FullName + "\"");
                ProcessUtilities.Run(coverageToolPath, "/session ALL /Save /As " + traceName);
            }
            else
            {
                ExecutionEventLog.RecordStatus("Clearing Coverage data for non-passing test.");
                ProcessUtilities.Run(coverageToolPath, "/Reset /session ALL");
            }
            ProcessUtilities.Run(coverageToolPath, "/Close /session ALL");
        }

        #endregion

        #region Merging and Uploading (Reporting)

        internal static void UploadResults(DirectoryInfo executionLogPath, string connectionString)
        {
            FileInfo[] covDatas = executionLogPath.GetFiles("*.covdata", SearchOption.AllDirectories);
            string coverageMergePath = Path.Combine(executionLogPath.FullName, "CodeCoverage");
            if (!Directory.Exists(coverageMergePath))
            {
                Directory.CreateDirectory(coverageMergePath);
            }

            ProcessUtilities.Run(coverageToolPath, "/Save /db " + connectionString);
        }


        /// <summary>
        /// Merges Code coverage results for a machine into a single set of results
        /// </summary>
        /// <param name="executionLogPath"></param>
        internal static void MergeSingleMachineResults(DirectoryInfo executionLogPath)
        {
            FileInfo[] covDatas = executionLogPath.GetFiles("*.covdata", SearchOption.AllDirectories);
            string coverageMergePath = Path.Combine(executionLogPath.FullName, "CodeCoverage");
            if (Directory.Exists(coverageMergePath))
            {
                Directory.Delete(coverageMergePath, true);
            }
            Directory.CreateDirectory(coverageMergePath);


            //Specify inputs to merge
            StringBuilder mergeInput = new StringBuilder();
            foreach (FileInfo covData in covDatas)
            {
                mergeInput.AppendLine(covData.FullName);
            }
            string inputsPath = Path.Combine(executionLogPath.FullName, "CoverageInputs.txt");
            File.WriteAllText(inputsPath, mergeInput.ToString());

            string command = "/I @" + inputsPath + " /O " + coverageMergePath;
            ExecutionEventLog.RecordStatus("Merging Coverage results: " + coverageDataToolPath + " " + command);
            if (ProcessUtilities.Run(coverageDataToolPath, command) != 0)
            {
                throw new ApplicationException("Code Coverage Merging failed");
            }

            ExecutionEventLog.RecordStatus("Clearing Redundant inputs coverage data.");
            DeleteCoverageData(covDatas);
        }

        /// <summary>
        /// Deletes multi-machine CodeCoverage data sets. For use after merging is complete.
        /// </summary>
        /// <param name="machineDirectories"></param>
        public static void DeleteCodeCoverageMergeInputs(IEnumerable<DirectoryInfo> machineDirectories)
        {
            foreach (DirectoryInfo machineDirectory in machineDirectories)
            {
                string machineCCPath = Path.Combine(machineDirectory.FullName, "CodeCoverage");
                DeleteCoverageData(new DirectoryInfo(machineCCPath).GetFiles("*.covdata", SearchOption.AllDirectories));
            }
        }

        /// <summary>
        /// Deletes Coverage Data and their parent directories (which is part of CC schema).
        /// Implementation intent: We achieve this by deleting the parent directories recursively, once per dir, to prevent collisions(ie- non existent file operation, after deletion).
        /// The coverage data is flatly stored in a single code coverage data directory under each test results data directory.
        /// </summary>
        /// <param name="covDatas"></param>
        private static void DeleteCoverageData(FileInfo[] covDatas)
        {
            List<string> directoryDeletionSet = new List<string>();

            foreach (FileInfo file in covDatas)
            {
                if (!directoryDeletionSet.Contains(file.DirectoryName))
                {
                    directoryDeletionSet.Add(file.DirectoryName);
                }
            }
            foreach (string directoryPath in directoryDeletionSet)
            {
                Directory.Delete(directoryPath, true);
            }
        }

        /// <summary>
        /// Merge Multi-machine CodeCoverage data Sets.
        /// </summary>        
        public static void MergeCodeCoverage(IEnumerable<DirectoryInfo> machineDirectories, DirectoryInfo distributionDirectory)
        {
            StringBuilder mergeInput = new StringBuilder();

            foreach (DirectoryInfo machinedirectory in machineDirectories)
            {
                //HACK: cross component policy- factor this into a code coverage component/namespace?
                string machineCCPath = Path.Combine(machinedirectory.FullName, "CodeCoverage");

                DirectoryInfo machinePath = new DirectoryInfo(machineCCPath);
                if (machinePath.Exists)
                {
                    FileInfo[] covDatas = machinePath.GetFiles("*.covdata", SearchOption.AllDirectories);

                    //Specify inputs to merge                
                    foreach (FileInfo covData in covDatas)
                    {
                        mergeInput.AppendLine(covData.FullName);
                    }
                }
            }

            string coverageMergePath = Path.Combine(distributionDirectory.FullName, "CodeCoverage");
            if (Directory.Exists(coverageMergePath))
            {
                Directory.Delete(coverageMergePath, true);
            }
            Directory.CreateDirectory(coverageMergePath);

            string inputsPath = Path.Combine(distributionDirectory.FullName, "CoverageInputs.txt");
            File.WriteAllText(inputsPath, mergeInput.ToString());

            string command = "/I @" + inputsPath + " /O " + coverageMergePath;
            CodeCoverageUtilities.DoMerge(command);
        }

        /// <summary>
        /// This is needed for merge to upload cc data to server
        /// </summary>
        /// <param name="logDirectory"></param>
        /// <param name="codeCoverageConnection"></param>        
        internal static void UploadCodeCoverage(DirectoryInfo logDirectory, string codeCoverageConnection)
        {
            string codeCoveragePath = Path.Combine(logDirectory.FullName, "CodeCoverage");
            string command = "/I \"" + codeCoveragePath + "\" /DB \"" + codeCoverageConnection + "\"";

            //Run this command in console- we don't do this during execution
            if (ProcessUtilities.Run(CodeCoverageUtilities.coverageDataToolPath, command, ProcessWindowStyle.Hidden, false) != 0)
            {
                throw new ApplicationException("Failed to upload code coverage results to server with:" + coverageDataToolPath + " " + command);
            }
        }

        internal static void DoMerge(string command)
        {
            if (ProcessUtilities.Run(CodeCoverageUtilities.coverageDataToolPath, command, ProcessWindowStyle.Hidden, false) != 0)
            {
                throw new ApplicationException("Code coverage Merge operation failed:" + coverageDataToolPath + " " + command);
            }
        }

        #endregion

        #region Validation / Pain Prevention

        /// <summary>
        /// Validates that Code Coverage settings and  parameters are coherent.
        /// </summary>
        /// <param name="enableCodeCoverage"></param>
        /// <param name="codeCoverageImport"></param>
        public static void ValidateForCodeCoverage(bool enableCodeCoverage, string codeCoverageImport)
        {
            if (enableCodeCoverage && string.IsNullOrEmpty(codeCoverageImport))
            {
                throw new ArgumentException("CodeCoverageImport must be specified when Code Coverage is enabled.");
            }
            if (!enableCodeCoverage && !string.IsNullOrEmpty(codeCoverageImport))
            {
                throw new ArgumentException("CodeCoverageImport is specified but Code Coverage is not enabled. Please review settings.");
            }
            if (enableCodeCoverage)
            {
                VerifyInstallation();
            }
        }

        internal static void VerifyInstallation()
        {
            if (!File.Exists(coverageToolPath))
            {
                throw new InvalidOperationException("Code coverage tool could not be found: " + coverageToolPath);
            }
            if (!File.Exists(coverageDataToolPath))
            {
                throw new InvalidOperationException("Code coverage data tool could not be found: " + coverageDataToolPath);
            }
        }

        #endregion
    }
}