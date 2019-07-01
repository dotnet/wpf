// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace Microsoft.Test.Logging
{
    /// <summary>
    /// </summary>
    public abstract class LoggerBase : ILogger
    {
        /// <summary />
        abstract public void BeginTest(string testName);

        /// <summary />
        abstract public void BeginVariation(string variationName);

        /// <summary />
        public string GetCurrentTestName()
        {
            throw new NotImplementedException();
        }

        /// <summary />
        public string GetCurrentVariationName()
        {
            throw new NotImplementedException();
        }

        /// <summary />
        public Result? GetCurrentVariationResult()
        {
            throw new NotImplementedException();
        }

        /// <summary />
        public int? GetCurrentTestLogResult()
        {
            throw new NotImplementedException();
        }

        /// <summary />
        public void SetCurrentTestLogResult(int result)
        {
            throw new NotImplementedException();
        }

        /// <summary />
        abstract public void EndTest(string testName);

        /// <summary />
        abstract public void EndVariation(string variationName);

        /// <summary />
        abstract public void LogFile(string filename);

        /// <summary />
        abstract public void LogMessage(string message);

        /// <summary />
        abstract public void LogObject(object payload);

        /// <summary />
        abstract public void LogProcess(int processId);

        /// <summary />
        abstract public void LogProcessCrash(int processId);

        /// <summary />
        abstract public void LogResult(Result result);

        /// <summary />
        virtual public void Reset()
        {
            // no-op
        }
    }
}
