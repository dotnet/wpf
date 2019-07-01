// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using System.IO;
using Microsoft.Test.Logging;

namespace Microsoft.Test.Execution.Logging
{
    /// <summary>
    /// Packages Test log data to TestRecord and VariationRecords
    /// Harness side starts each test sesion via RegisterTest, and the remote test closes the session with EndTest.
    /// Recording Logger enforces correct test semantics by recording violations as failures.
    /// </summary>
    internal class LoggerCollection : Collection<ILogger>, ILogger
    {
        #region ILogger Members

        public void BeginTest(string testName)
        {
            foreach (ILogger logger in this)
            {
                logger.BeginTest(testName);
            }
        }

        public void BeginVariation(string variationName)
        {
            foreach (ILogger logger in this)
            {
                logger.BeginVariation(variationName);
            }
        }

        public void EndTest(string testName)
        {
            foreach (ILogger logger in this)
            {
                logger.EndTest(testName);
            }
        }

        public void EndVariation(string variationName)
        {
            foreach (ILogger logger in this)
            {
                logger.EndVariation(variationName);
            }
        }

        public void LogFile(string filename)
        {
            foreach (ILogger logger in this)
            {
                logger.LogFile(filename);
            }
        }

        public void LogMessage(string message)
        {
            foreach (ILogger logger in this)
            {
                logger.LogMessage(message);
            }
        }

        public void LogObject(object o)
        {
            foreach (ILogger logger in this)
            {
                logger.LogObject(o);
            }
        }

        public void LogProcess(int ProcessId)
        {
            foreach (ILogger logger in this)
            {
                logger.LogProcess(ProcessId);
            }
        }

        public void LogProcessCrash(int processId)
        {
            foreach (ILogger logger in this)
            {
                logger.LogProcessCrash(processId);
            }
        }

        public void LogResult(Result result)
        {
            foreach (ILogger logger in this)
            {
                logger.LogResult(result);
            }
        }

        public string GetCurrentTestName()
        {
            throw new NotImplementedException();
        }

        public string GetCurrentVariationName()
        {
            throw new NotImplementedException();
        }

        public Result? GetCurrentVariationResult()
        {
            throw new NotImplementedException();
        }

        public int? GetCurrentTestLogResult()
        {
            throw new NotImplementedException();
        }

        public void SetCurrentTestLogResult(int result)
        {
            throw new NotImplementedException();
        }

        public void Reset()
        {
            foreach (ILogger logger in this)
            {
                logger.Reset();
            }
        }

        #endregion
    }
}