// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Security.Permissions;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.Test.Logging
{
    /// <summary>
    /// communication contract for logging.   
    /// </summary>    
    public interface ILogger
    {
        #region Log Reporting Methods

        /// <summary/>        
        void BeginTest(string testName);

        /// <summary/>                
        void EndTest(string testName);

        /// <summary/>
        void BeginVariation(string variationName);

        /// <summary/>
        void EndVariation(string variationName);

        /// <summary/>
        void LogFile(string filename);

        /// <summary/>
        void LogMessage(string message);

        /// <summary/>
        void LogObject(Object payload);

        /// <summary/>
        void LogResult(Result result);

        /// <summary/>
        void LogProcessCrash(int processId);

        /// <summary/>        
        void LogProcess(int processId);

        #endregion

        #region State Query Methods

        /// <summary/>
        string GetCurrentTestName();

        /// <summary/>
        string GetCurrentVariationName();

        /// <summary/>
        Result? GetCurrentVariationResult();

        #endregion

        #region Legacy TestLog Interop Caching

        // These operate on an int because the real legacy result type is an
        // enum. Since InternalUtilities doesn't have a reference to TestRuntime,
        // we can't reference that enum explicitly.

        /// <summary/>
        int? GetCurrentTestLogResult();

        /// <summary/>
        void SetCurrentTestLogResult(int result);

        #endregion

        #region State Reset

        /// <summary>
        /// Tells an ILogger to reset internal state, behavior which varies on
        /// a per-logger basis. 
        /// </summary>
        void Reset();

        #endregion
    }
}
