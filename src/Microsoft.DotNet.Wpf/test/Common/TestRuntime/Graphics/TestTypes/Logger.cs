// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Test.Logging;

namespace Microsoft.Test.Graphics.TestTypes
{
    /// <summary>
    /// Logging encapsulation for CoreGraphics tests
    /// </summary>
    public class Logger
    {
        /// <summary/>
        public Logger(string name)
        {
            log = new TestLog(name);
            log.Result = TestResult.Pass;
        }

        /// <summary/>
        public static Logger Create()
        {
            return Create("GraphicsTest");
        }

        /// <summary/>
        [Logging.LoggingSupportFunction]
        public static Logger Create(string name)
        {           
            return new Logger(name);
        }

        /// <summary/>
        [Logging.LoggingSupportFunction]
        public static void Log(string message)
        {
            GlobalLog.LogStatus(message);
        }

        /// <summary/>
        [Logging.LoggingSupportFunction]
        public static void LogFinalResults(int fail, int total)
        {
            LogFinalResults(fail, total, false);
        }

        /// <summary/>
        [Logging.LoggingSupportFunction]
        public static void LogFinalResults(int fail, int total, bool isRunAll)
        {
            string message = string.Format("{0} of {1} variations passed", total - fail, total);

            GlobalLog.LogEvidence(message);            
        }

        /// <summary/>
        [Logging.LoggingSupportFunction]
        public static void LogRunAllResults(int fail, int total)
        {
            string message = string.Empty;
            if (fail == 0)
            {
                message = "PASS - All tests passed";
            }
            else
            {
                message = string.Format("FAIL - {0} out of {1} tests passed", total - fail, total);
            }
            GlobalLog.LogEvidence(message);            
        }
   
        /// <summary/>
        [Logging.LoggingSupportFunction]
        public void LogStatus(string message)
        {
            log.LogStatus(message);
        }

        /// <summary/>
        [Logging.LoggingSupportFunction]
        public void AddFailure(string message)
        {
            log.Result = TestResult.Fail;
            log.LogEvidence(message);
        }


        /// <summary/>
        [Logging.LoggingSupportFunction]
        public void Close()
        {
            log.Close();
        }

        private TestLog log;
    }
}