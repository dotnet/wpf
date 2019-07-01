// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Microsoft.Test.Execution
{

    /// <summary>
    /// Execution Backup Store is responsible for storing  backup information of execution results.
    /// These provides incremental anchor points for results recovery, from last know good point before unexpected infra termination, which can arise from:
    ///   -GPU Timeout
    ///   -External process termination
    ///   -Infra leaks/Crash (Hypothetical presently - no active infra crash bugs)
    /// </summary>
    public static class ExecutionBackupStore
    {
        private static readonly string serializationFileName = "TestRecords_ExecutionBackup.xml";

        /// <summary>
        /// Restore intermediate test results.
        /// </summary>
        /// <param name="executionLogPath"></param>
        /// <returns></returns>
        internal static List<TestRecord> LoadIntermediateTestRecords(DirectoryInfo executionLogPath)
        {
            List<TestRecord> results = null;
            FileInfo resultsFileInfo = new FileInfo(Path.Combine(executionLogPath.FullName, serializationFileName));
            if (resultsFileInfo.Exists)
            {
                try
                {
                    using (XmlTextReader textReader = new XmlTextReader(resultsFileInfo.OpenRead()))
                    {
                        results = (List<TestRecord>)ObjectSerializer.Deserialize(textReader, typeof(List<TestRecord>), null);
                    }
                }
                catch (Exception e)
                {
                    ExecutionEventLog.RecordException(e);
                }
            }
            return results;
        }

        /// <summary>
        /// Save the backup test results.
        /// </summary>        
        internal static void SaveIntermediateTestRecords(List<TestRecord> tests, DirectoryInfo executionLogPath)
        {
            try
            {
                FileInfo resultsFileInfo = new FileInfo(Path.Combine(executionLogPath.FullName, serializationFileName));
                using (XmlTextWriter textWriter = new XmlTextWriter(resultsFileInfo.Create(), System.Text.Encoding.UTF8))
                {
                    textWriter.Formatting = Formatting.Indented;
                    ObjectSerializer.Serialize(textWriter, tests);
                }
            }
            catch (Exception e)
            {
                ExecutionEventLog.RecordException(e);
            }
        }

        /// <summary>
        /// Clear all the accumulated backup test results. This should be applied once the results have been successfully stored.
        /// </summary>        
        public static void ClearAllIntermediateTestResults(DirectoryInfo executionLogPath)
        {
            // Since EnumerateFiles is new for 4.0, have to use a different approach to delete these files in 3.5 build.
#if TESTBUILD_CLR40
            foreach (FileInfo file in executionLogPath.EnumerateFiles(serializationFileName, SearchOption.AllDirectories))
            {
                file.Delete();
            }
#endif
#if TESTBUILD_CLR20
            foreach (string filePath in Directory.GetFiles(executionLogPath.FullName, serializationFileName, SearchOption.AllDirectories))
            {
                try
                {
                    File.Delete(filePath);
                }
                catch { }; // Maybe log this?
            }
#endif

        }

    }
}