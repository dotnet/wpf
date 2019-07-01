// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Test.Logging;

namespace Microsoft.Test.Graphics.TestTypes
{
    /// <summary>
    /// Base class for CoreGraphics tests
    /// </summary>
    public abstract class CoreGraphicsTest
    {

        /// <summary>
        /// Initializes the test with variation specific data
        /// </summary>
        public virtual void Init(Variation v)
        {
            variation = v;
            failures = 0;

            string max = v["MaxLogFails"];
            maxLogFails = (max == null) ? 50 : StringConverter.ToInt(max);

            string pri = v["Priority"];
            priority = (pri == null) ? 0 : StringConverter.ToInt(pri);

            failOnPurpose = (v["Fail"] == "true") ? true : false;

            string prefix = v["LogFilePrefix"];
            if (prefix == null)
            {
                prefix = this.GetType().Name;
            }
            logPrefix = prefix + "_" + TwoDigitVariationID(v.ID);

            DateTime current = DateTime.Now;
            logger = Logger.Create("Variation " + TwoDigitVariationID(v.ID));
            if (TestLauncher.LogTime)
            {
                if (!loggedFirst)
                {
                    DateTime logCreateTime = DateTime.Now;
                    Log("Test Starting...");
                    DateTime firstLogTime = DateTime.Now;
                    Log("  BeginTime:        {0:HH:mm:ss.fff}", TestLauncher.BeginTime);
                    Log("  LogCreationTime:  {0:HH:mm:ss.fff}", logCreateTime - current);
                    Log("  FirstLogTime:     {0:HH:mm:ss.fff}", firstLogTime - logCreateTime);
                    loggedFirst = true;
                }
                current = DateTime.Now;
                Log("  TotalTestTime:    {0:HH:mm:ss.fff}", current - TestLauncher.BeginTime);
                Log("");
            }

            if (v["Description"] != null)
            {
                Log("Test Description:");
                Log("-----------------");
                Log("  " + v["Description"]);
                Log("");
            }
            if (v["Expectation"] != null)
            {
                Log("Expected Output:");
                Log("----------------");
                Log("  " + v["Expectation"]);
                Log("");
            }
        }

        /// <summary>
        /// Contains the implementation for execution of the test variation
        /// </summary>
        public abstract void RunTheTest();

        /// <summary>
        /// Returns the test variation ID
        /// </summary>
        protected string TwoDigitVariationID(int id)
        {
            // We assume that no one has more than 99 variations in a script
            //  (I sure hope not!)

            if (id < 10)
            {
                return "0" + id;
            }
            return id.ToString();
        }

        /// <summary>
        /// Adds a log entry
        /// </summary>
        public void Log(string fmt, params object[] args)
        {
            logger.LogStatus(string.Format(fmt, args));
        }

        /// <summary>
        /// Adds a failure entry to the log
        /// </summary>
        public void AddFailure(string fmt, params object[] args)
        {
            if (failures <= maxLogFails)  // This is to prevent logging the same error a million times
            {
                failures++;
                logger.LogStatus("");
                logger.AddFailure(string.Format("!!  " + fmt, args));
            }
        }

        /// <summary>
        /// Indicates the number of failures encountered
        /// </summary>
        public int Failures { get { return failures; } }

        internal void LogResult()
        {
            Log("");
            if (failures == 0)
            {
                Log("PASS - Variation " + variation.ID + " passed.");
            }
            else
            {
                Log("FAIL - Variation {0} failed", variation.ID, failures);
                variationsFailed++;
            }
            Log("==============================");
            Log("==============================");
            Log("");
            logger.Close();
        }

        internal static bool EndTheTest()
        {
            if (RenderingTest.window != null)
            {
                LogManager.LogMessageDangerously("Disposing the window");
                RenderingTest.window.Dispose();
                RenderingTest.window = null;
                LogManager.LogMessageDangerously("Done");
            }

            Logger.LogFinalResults(variationsFailed, Variation.TotalVariations, GraphicsTestLoader.IsRunAll);

            if (variationsFailed == 0)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Delegate for testing code expected to throw Exceptions
        /// </summary>
        protected delegate void ExceptionThrower();

        /// <summary>
        /// Reports a failure if the expected exception is not thrown from the callback
        /// </summary>
        protected void Try(ExceptionThrower callback, Type expectedExceptionType)
        {
            try
            {
                callback();
                AddFailure("Expected " + expectedExceptionType.Name + " from " + callback.Method.Name);
            }
            catch (Exception ex)
            {
                Type t = ex.GetType();
                if (!t.Equals(expectedExceptionType) && !t.IsSubclassOf(expectedExceptionType))
                {
                    AddFailure("Incorrect exception thrown for " + callback.Method.Name);
                    Log("*** Expected: " + expectedExceptionType.Name);
                    Log("***   Actual: " + t.Name);
                }
            }
        }

        /// <summary>
        /// Delegate for testing code expected to execute without error
        /// </summary>
        protected delegate void SafeExecutionBlock();

        /// <summary>
        /// Reports a failure if an exception is thrown from the SafeExecutionBlock
        /// </summary>
        protected void SafeExecute(SafeExecutionBlock callback)
        {
            try
            {
                callback();
            }
            catch (Exception ex)
            {
                AddFailure("Unexpected exception thrown during execution of " + callback.Method.Name);
                Log(ex.GetType() + ": " + ex.Message);
            }
        }

        /// <summary>
        /// Contains the test variation parameters
        /// </summary>
        protected Variation variation;
        private int maxLogFails;

        /// <summary>
        /// Test Execution priority
        /// </summary>
        protected int priority;

        /// <summary>
        /// Test output logging/results filename prefix
        /// </summary>
        protected string logPrefix;

        /// <summary>
        /// This is to verify logging and just in case both the devs and
        /// test did the same thing wrong.
        /// </summary> 
        protected bool failOnPurpose;
        private int failures;
        private Logger logger;

        internal static int variationsFailed = 0;
        private static bool loggedFirst = false;
    }
}
