// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security.Permissions;
using System.Xml;
using System.IO;
using System.Security;

namespace Microsoft.Test.Logging
{    
    /// <summary>
    /// Singleton class that handles:
    ///     the client connectionto the logging service
    ///     Log creation / test lifetime
    ///     sending Log calls over the Trace system
    /// </summary>
    // LogManager owns an IDisposable field but doesn't implement IDisposable,
    // but that is ok because this is a singleton, so you don't ever dispose it.
    // Only drivers should need to refer to LogManager - individual tests should
    // just be able to refer to Log.Current, which is the test consumer API.
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001")]
    public class LogManager
    {
        #region Creation/Destruction Code (Not Public)

        private LogManager()
        {
        }

        static LogManager()
        {
            instance = new LogManager();
        }

        // This finalizer is a very bad idea. Logging involves a connection oriented api which is prone to fail.
        // Finalizers should never throw exceptions. We should clean up this api so the finalizer is exception safe.
        // Are there any concrete issues preventing a move to disposable pattern?
        // Dispose is called by user, which puts error handling responsability in an obvious place. Here, we don't have that control.
        ~LogManager()
        {            
            if (traceToLoggerAdaptor != null)
            {
                try
                {
                    // Do we need a separate close operation. Trace has it's own close semantic- these should probably be distinct.
                    logger.Close();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Our logging connection is hosed. Infra is most likely offline. Dumping error to console for now." + e);
                }
            }
        }
        #endregion

        #region Public API

         /// <summary/>
        public static LogManager Instance
        {
            get
            {
                if (!initialized)
                {
                    instance.Initialize();
                }
                return instance;
            }
        }

        /// <summary>
        /// The current active log.  This will be null if there is no active
        /// test. This is a wrapper around Log.Current, which is how you should
        /// access the current log - this getter will be removed in the future.
        /// </summary>
        public static Log CurrentLog
        {
            get { return Log.Current; }
        }

        /// <summary>
        /// Begins test logging.
        /// </summary>
        public static Log BeginTest(string testName)
        {
            // Short term workaround to handle that existing tests are calling BeginTest to hookup because we made it that way...
            if (Log.Current != null && Log.Current.Name == testName)
            {
                return Log.Current;
            }

            // '_QVDEBUG' is a handshake from the infra to let us know that we
            // are in the midst of debugging. In this case, we want BeginTest to
            // null out Log/Variation.Current which were initally wired up to the
            // existing log/variation, but in the debug case are actually leftovers
            // from the previous debug execution, and we want to start fresh.
            if (Log.Current != null && Log.Current.Name == testName + "_QVDEBUG")
            {
                Log.Current = null;
                Variation.Current = null;
            }

            if (Log.Current != null)
            {
                throw new InvalidOperationException("You must end the previous test before beginning a new one");
            }

            return new Log(testName, Instance.logger);
        }

        /// <summary>
        /// Ends the current test, throws if there is no active
        /// Log.  Sets LogManager.CurrentLog to null
        /// </summary>
        public static void EndTest()
        {
            if (Log.Current == null)
            {
                throw new InvalidOperationException("You must call BeginTest before EndTest");
            }
            Log.Current.Close();
        }

        /// <summary>
        /// Allows a file log call to be made outside of the context of a variation.
        /// This is for drivers who need to log before/after a test - tests should
        /// always log via Variation.
        /// </summary>
        public static void LogFileDangerously(FileInfo file)
        {
            Instance.logger.LogFile(file.FullName);
        }

        /// <summary>
        /// Allows a message log call to be made outside of the context of a variation.
        /// This is for drivers who need to log before/after a test - tests should
        /// always log via Variation.
        /// </summary>
        public static void LogMessageDangerously(string message)
        {
            Instance.logger.LogMessage(message);
        }

        /// <summary>
        /// Allows a message log call to be made outside of the context of a variation.
        /// This is for drivers who need to log before/after a test - tests should
        /// always log via Variation.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704")]
        public static void LogProcessDangerously(int pid)
        {
            Instance.logger.LogProcess(pid);
        }

        #region Legacy TestLog interop

        // Legacy TestLog needs to track the 'current' TestResult because of
        // it's write-many behavior. These methods allow TestLog to get/set
        // a TestResult value across the pipe. The value type is int because
        // TestResult is a legacy enum that InternalUtilities can't reference
        // (defined in TestRuntime). Ultimately we'd like to remove TestLog and
        // therefore these support methods, but it is not feasible to do so at
        // this time. These methods need to be public so TestLog inside
        // TestRuntime can use them, but nobody else should - hence the not-so-
        // friendly summary tags.

        /// <summary>
        /// If you have to ask, don't use it.
        /// </summary>
        public static int? GetCurrentTestLogResult()
        {
            return Instance.logger.GetCurrentTestLogResult();
        }

        /// <summary>
        /// If you have to ask, don't use it.
        /// </summary>
        public static void SetCurrentTestLogResult(int result)
        {
            Instance.logger.SetCurrentTestLogResult(result);
        }

        #endregion

        #endregion

        #region Private Implementation

#if CLR_30_SECURITY
        [SecuritySafeCritical]
        #endif
        [PermissionSet(SecurityAction.Assert, Name = "FullTrust")]
        [SuppressMessage("Microsoft.Security", "CA2106:SecureAsserts")]
        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Justification = "Sam can take a closer look at this.")]
        private void Initialize()
        {
            traceToLoggerAdaptor = new TraceToLoggerAdaptor();

            //Have the CTL send its logs out to the infra via the logging Client
            logger = LogContract.ConnectClient();
            traceToLoggerAdaptor.Logger = logger;

            //Register the CTL to intercept trace messages
            Trace.Listeners.Add(traceToLoggerAdaptor);

            //Disable tracing of Fail to UI
            Trace.Listeners.Remove("Default");
            initialized = true;

            WireUp();
        }

        /// <summary>
        /// Queries the logging channel to see if there is already a current
        /// test or variation, and if so creates the appropriate objects. This
        /// occurs in a cross-process scenario where a parent process created
        /// a Log. This ensures that Log.Current and Variation.Current will
        /// be non-null as appropriate in the child process.
        /// </summary>
        private void WireUp()
        {
            if (logger.GetCurrentTestName() != null)
            {
                new Log(logger.GetCurrentTestName(), logger);
            }
            if (logger.GetCurrentVariationName() != null)
            {
                new Variation(logger.GetCurrentVariationName(), logger);
            }
        }
        
        #endregion

        #region Private fields

        private static readonly LogManager instance;
        private static bool initialized = false;
        private TraceToLoggerAdaptor traceToLoggerAdaptor;
        private LoggingClient logger;

        #endregion
    }
}
