// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using StreamJsonRpc;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Security;
using System.Security.Permissions;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Test.Logging
{
    /// <summary>
    /// The client side implementation for the logging system.
    /// </summary>
    public class LoggingClient : ILogger
    {
        public static JsonRpc Server { get; set; } //Recipient of logging                

        /// <summary>
        /// 
        /// </summary>
        internal LoggingClient(JsonRpc server)
        {
            Server = server;
        }

        /// <summary>
        /// Allows closure of the Client connection.
        /// </summary>
#if CLR_40
    [SecuritySafeCritical]
#endif
#if CLR_20
    [SecurityCritical]
    [SecurityTreatAsSafe]
#endif
        public void Close()
        {
            LogContract.DisconnectClient(this);
        }

        public void BeginTest(string testName)
        {
            AsyncHelper.RunSync(() => Server.InvokeAsync("BeginTest", testName));
        }

        public void EndTest(string testName)
        {
            AsyncHelper.RunSync(() => Server.InvokeAsync("EndTest", testName));
        }

        public void BeginVariation(string variationName)
        {
            AsyncHelper.RunSync(() => Server.InvokeAsync("BeginVariation", variationName));
        }

        public void EndVariation(string variationName)
        {
            AsyncHelper.RunSync(() => Server.InvokeAsync("EndVariation", variationName));
        }

        public void LogFile(string filename)
        {
            AsyncHelper.RunSync(() => Server.InvokeAsync("LogFile", filename));
        }

        public void LogMessage(string message)
        {
            AsyncHelper.RunSync(() => Server.InvokeAsync("LogMessage", message));
        }

        public void LogObject(object payload)
        {
            AsyncHelper.RunSync(() => Server.InvokeAsync("LogObject", payload));
        }

        public void LogResult(Result result)
        {
            AsyncHelper.RunSync(() => Server.InvokeAsync("LogResult", result));
        }

        public void LogProcessCrash(int processId)
        {
            AsyncHelper.RunSync(() => Server.InvokeAsync("LogProcessCrash", processId));
        }

        public void LogProcess(int processId)
        {
            AsyncHelper.RunSync(() => Server.InvokeAsync("LogProcess", processId));
        }

        public string GetCurrentTestName()
        {
            return AsyncHelper.RunSync<string>(() => Server.InvokeAsync<string>("GetCurrentTestName"));
        }

        public string GetCurrentVariationName()
        {
            return AsyncHelper.RunSync<string>(() => Server.InvokeAsync<string>("GetCurrentVariationName"));
        }

        public Result? GetCurrentVariationResult()
        {
            try
            {
                return AsyncHelper.RunSync<Result?>(() => Server.InvokeAsync<Result?>("GetCurrentVariationResult"));
            }
            catch (InvalidOperationException ioe) when (ioe.Message == "null result is not assignable to a value type.")
            {
                // TODO:  Remove this.  We eat this exception due to a bug in StreamJsonRpc.  This is already fixed, just waiting on a new package.
                // See: https://github.com/Microsoft/vs-streamjsonrpc/issues/237
                return null;
            }
        }

        public int? GetCurrentTestLogResult()
        {
            try
            {
                return AsyncHelper.RunSync<int?>(() => Server.InvokeAsync<int?>("GetCurrentTestLogResult"));
            }
            catch (InvalidOperationException ioe) when (ioe.Message == "null result is not assignable to a value type.")
            {
                // TODO:  Remove this.  We eat this exception due to a bug in StreamJsonRpc.  This is already fixed, just waiting on a new package.
                // See: https://github.com/Microsoft/vs-streamjsonrpc/issues/237
                return null;
            }
        }
        public void SetCurrentTestLogResult(int result)
        {
            AsyncHelper.RunSync(() => Server.InvokeAsync("SetCurrentTestLogResult", result));
        }

        public void Reset()
        {
            AsyncHelper.RunSync(() => Server.InvokeAsync("Reset"));
        }
    }
}
