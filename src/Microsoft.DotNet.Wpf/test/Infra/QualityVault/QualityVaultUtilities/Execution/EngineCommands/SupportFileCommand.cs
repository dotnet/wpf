// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Microsoft.Test.Execution.EngineCommands
{
    /// <summary>
    /// Performs support file binplacement for tests.
    /// </summary>
    internal class SupportFileCommand : SimpleCleanableCommand
    {
        private SupportFileCommand() { }

        public static SupportFileCommand Apply(List<TestRecord> tests, DirectoryInfo testBinariesDirectory, DirectoryInfo executionDirectory)
        {
            ExecutionEventLog.RecordStatus("STARTED  : -------------- Copiying Support Files --------------- |");
            CopySupportFiles(tests, testBinariesDirectory, executionDirectory);
            ExecutionEventLog.RecordStatus("COMPLETED: -------------- Copiying Support Files --------------- |");
            
            return new SupportFileCommand();
        }


        /// <summary>
        /// Copies support files. Throws if files are present already
        /// </summary>
        /// <param name="tests"></param>
        /// <param name="sourceDirectory"></param>
        /// <param name="destinationRootDirectory"></param>
        internal static void CopySupportFiles(List<TestRecord> tests, DirectoryInfo sourceDirectory, DirectoryInfo destinationRootDirectory)
        {
            //TODO: Support wildcards/dirs
            ThrowIfRelativePath(sourceDirectory);
            ThrowIfRelativePath(destinationRootDirectory);

            Collection<TestSupportFile> supportFiles;
            // In the normal case of not having a named execution group, all
            // tests share the same set of support files, so we can just grab
            // the first test's set.
            if (String.IsNullOrEmpty(tests.First().TestInfo.ExecutionGroup))
            {
                supportFiles = tests.First().TestInfo.SupportFiles;
            }
            // For a named execution group there can be distinct support files,
            // so we need to merge each test's set of support files.
            else
            {
                supportFiles = new Collection<TestSupportFile>();
                foreach (TestRecord test in tests)
                {
                    foreach (TestSupportFile supportFile in test.TestInfo.SupportFiles)
                    {
                        if (!supportFiles.Contains(supportFile))
                        {
                            supportFiles.Add(supportFile);
                        }
                    }
                }
            }

            foreach (TestSupportFile file in supportFiles)
            {
                ExecutionEventLog.RecordStatus("Copying File: " + file.Source);
                string source = Path.Combine(sourceDirectory.FullName, file.Source);

                source = ReplaceSupportFileVariables(source);
                DirectoryInfo destinationDirectory;
                // If the support file specifies an absolute path, that is our destination.
                if (Path.IsPathRooted(file.Destination))
                {
                    destinationDirectory = new DirectoryInfo(file.Destination);
                }
                // If the support file specified a path but it wasn't absolute, then it is
                // relative to the destination base path.
                else if (!string.IsNullOrEmpty(file.Destination))
                {
                    destinationDirectory = new DirectoryInfo(Path.Combine(destinationRootDirectory.FullName, file.Destination));
                }
                // Otherwise the support file didn't specify a destination, so it goes to the base
                else
                {
                    destinationDirectory = destinationRootDirectory;
                }

                // Given we've determined the destination, make sure the directory exists.
                if (!destinationDirectory.Exists)
                {
                    destinationDirectory.Create();
                }

                if (source.Contains("*")) //Copy * Wildcard
                {
                    ProcessUtilities.Run("cmd", string.Format(CultureInfo.InvariantCulture, "/c copy /y \"{0}\" \"{1}\"", source, destinationDirectory.FullName));
                }
                else if (Directory.Exists(source))  //Copy Directory
                {
                    ProcessUtilities.Run("cmd", string.Format(CultureInfo.InvariantCulture, "/c xcopy /ey /q \"{0}\" \"{1}\"\\", source, destinationDirectory.FullName));
                }
                else         //Copy individual File
                {
                    string destinationFile = Path.Combine(destinationDirectory.FullName, Path.GetFileName(source));
                    File.Copy(source, destinationFile, true);
                    File.SetAttributes(destinationFile, FileAttributes.Normal);
                }
            }
        }

        /// <summary>
        /// Replace special binplace strings
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        private static string ReplaceSupportFileVariables(string source)
        {
            return source.Replace("%PROCESSOR_ARCHITECTURE%", Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE"));
        }
        
        private static void ThrowIfRelativePath(DirectoryInfo path)
        {
            if (!Path.IsPathRooted(path.FullName))
            {
                throw new ArgumentException("path string must be an absolute path");
            }
        }
    }
}