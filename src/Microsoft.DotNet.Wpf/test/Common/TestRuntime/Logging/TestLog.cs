// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Permissions;

namespace Microsoft.Test.Logging
{    
    /// <summary>
    /// TestLog repersents the result produced by a single test or variation
    /// </summary>
    /// <remarks>
    /// You should create a test log for each test result.
    /// If you want to log multiple results you should create a VariationContext
    /// and then create a seperate TestLog for each result.
    /// You must call Close() when you are done logging the result.
    /// </remarks>
    [Serializable]
    [PermissionSet(SecurityAction.Assert, Name = "FullTrust")]
    public sealed class TestLog : IDisposable
    {
        #region Constructors        

        static TestLog()
        {
            resultMapping[TestResult.Pass] = Microsoft.Test.Result.Pass;
            resultMapping[TestResult.Fail] = Microsoft.Test.Result.Fail;
            resultMapping[TestResult.Ignore] = Microsoft.Test.Result.Ignore;
            resultMapping[TestResult.NotRun] = Microsoft.Test.Result.Fail;
            resultMapping[TestResult.Unknown] = Microsoft.Test.Result.Fail;
        }

        /// <summary>
        /// Creates a new TestLog
        /// </summary>
        /// <param name="name">Name of the current Test Variation.</param>        
        public TestLog(string name)
        {
            if (instance != null)
            {
                throw new InvalidOperationException("Nested testlog, badness ensues.");
            }
            // Since TestLog is just a legacy wrapper that equates to Variation,
            // if Variation.Current isn't null we have a problem. This will only
            // occur if a tester is mixing and matching the old and new logging
            // APIs, which is loosely supported but not recommended.
            if (Variation.Current != null)
            {
                throw new InvalidOperationException("Variation already existed.");
            }

            instance = this;
            Log.Current.CreateVariation(name);
            Variation.Current.VariationClosed += new Variation.VariationClosingEventHandler(VariationClosing);
        }

        private TestLog()
        {
        }

        #endregion

        #region Public Members

        /// <summary>
        /// Gets the Name of the TestLog
        /// </summary>     
        public string Name { get { return Variation.Current.Name; } }

        /// <summary>
        /// Gets the FullName of the TestLog
        /// </summary>        
        public string FullName
        {
            get { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Get or Sets the result of a test
        /// </summary>
        /// <value>a value indicating the result of the test</value>        
        public TestResult Result
        {
            get
            {
                // Query the logging pipe for the current TestLog result (allows
                // cross process to work). If there is a set result we return
                // it, otherwise we return Unknown.
                int? remoteResult = LogManager.GetCurrentTestLogResult();
                if (remoteResult == null)
                {
                    return TestResult.Unknown;
                }
                else
                {
                    return (TestResult)remoteResult.Value;
                }
            }
            set
            {
                TestResult currentResult = Result;

                //Don't allow a Pass result to override a non-pass result
                if (value == TestResult.Pass && (currentResult != TestResult.Unknown && currentResult != TestResult.Pass))
                {
                    Variation.Current.LogMessage("Ignoring attempt to override log result from : " + currentResult + " to:" + value);                    
                }
                else
                {
                    LogManager.SetCurrentTestLogResult((int)value);                
                }
            }
        }

        /// <summary>
        /// Gets or sets the Stage of the running test
        /// </summary>
        /// <value>a value indicating the Stage of the test</value>        
        public TestStage Stage
        {
            get { throw new NotImplementedException(); }
            set { }//No-Op 
        }

        /// <summary />
        public bool IsCompleted
        {
            //get { return (state & TestStates.Completed) != 0; }
            get { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Relative path to directory containing all files and logs for this Test.
        /// </summary>
        public string LogDirectory
        {
            get { throw new NotImplementedException("Logging is a one way pipe. We don't support this. Log your files to current executing directory."); }
        }

        /// <summary>
        /// Logs an existing file with the result of the test
        /// </summary>
        /// <param name="filename">name of the file to log</param>
        /// <remarks>
        /// You should call this API if you want to add files to the logs for later analysis.
        /// </remarks>
        [PermissionSet(SecurityAction.Assert, Name = "FullTrust")]
        public void LogFileDeferred(string filename)
        {
            if (filename == null)
                throw new ArgumentNullException("filename");
            if (!File.Exists(filename))
                throw new FileNotFoundException("The specified file could not be found", filename);

            Variation.Current.LogFile(new FileInfo(filename));
        }

        /// <summary>
        /// Creates a new file to log with the result of the test using the specified filename
        /// </summary>
        /// <param name="filename">filename of the file to log</param>        
        [PermissionSet(SecurityAction.Assert, Name = "FullTrust")]
        public void LogFile(string filename)
        {
            if (filename == null)
                throw new ArgumentNullException("filename");
            if (!File.Exists(filename))
                throw new FileNotFoundException("The specified file could not be found", filename);

            Variation.Current.LogFile(new FileInfo(filename));
        }

        /// <summary>
        /// Creates a new file to log with the result of the test using the specified stream and filename
        /// </summary>
        /// <param name="filename">filename of the file to log</param>
        /// <param name="stream">stream containing the contents of the file</param>
        /// <remarks>
        /// You should use this API if you have data you want to log in a file
        /// but do not want to write it to disk.  This can be useful when running
        /// in a secure environment.
        /// </remarks>
        [PermissionSet(SecurityAction.Assert, Name = "FullTrust")]
        public void LogFile(string filename, Stream stream)
        {
            FileInfo info =new FileInfo(filename);
            FileStream file = info.Open(FileMode.Create,FileAccess.Write);

            stream.Position = 0;
            byte[] buffer = new byte[stream.Length];
            stream.Read(buffer, 0, (int)stream.Length);
            file.Write(buffer, 0, (int)stream.Length);
            file.Close();

            Variation.Current.LogFile(info);
        }

        /// <summary>
        /// Logs a Comment Trace to the TestLog
        /// </summary>
        /// <param name="message">trace message to log</param>
        /// <remarks>
        /// Comments are the summary result of what the test did.
        /// You should log actions your test has done and results of validation as comments.
        /// This comments are agregated together and availible via the Comments property.
        /// It is not nessesary to log a commont indicating that the Test Passed as this is done for you.
        /// </remarks>
        [LoggingSupportFunction]
        public void LogEvidence(string message)
        {
            Variation.Current.LogMessage(message, null);            
        }


        /// <summary>
        /// Logs a Comment Trace to the TestLog
        /// </summary>
        /// <param name="message">trace message to log</param>
        /// <param name="args">arguments inside the message</param>        
        [LoggingSupportFunction]
        public void LogEvidence(string message, params object[] args)
        {
            Variation.Current.LogMessage(message, args);
        }

        /// <summary>
        /// Logs an exception trace to the Current or Global TestLog
        /// </summary>
        /// <param name="exception">excpetion to log</param>
        public void LogEvidence(Exception exception)
        {
            Variation.Current.LogMessage(exception.ToString(), null);
        }

        /// <summary>
        /// Logs an exception trace along with a message to the Current or Global TestLog
        /// </summary>
        /// <param name="exception">excpetion to log</param>
        /// <param name="message">excpetion to log</param>
        public void LogEvidence(Exception exception, string message)
        {
            if (exception == null)
                throw new ArgumentNullException("exception");

            Variation.Current.LogMessage(exception.ToString() + "\n" + message, null);
        }

        /// <summary>
        /// Logs a Status trace to the TestLog
        /// </summary>
        /// <param name="message">trace message to log</param>
        [LoggingSupportFunction]
        public void LogStatus(string message)
        {
            Variation.Current.LogMessage(message, null);
        }

        /// <summary>
        /// Logs a Status trace to the TestLog
        /// </summary>
        /// <param name="message">trace message to log</param>
        /// <param name="args">arguments inside the message</param>
        [LoggingSupportFunction]
        public void LogStatus(string message, params object[] args)
        {
            Variation.Current.LogMessage(message, args);
        }

        /// <summary>
        /// Logs a Debug trace to the TestLog
        /// </summary>
        /// <param name="message">trace message to log</param>
        [LoggingSupportFunction]
        public void LogDebug(string message)
        {
            Variation.Current.LogMessage(message, null);
        }

        /// <summary>
        /// Logs a Debug trace to the TestLog
        /// </summary>
        /// <param name="message">trace message to log</param>
        /// <param name="args">arguments inside the message</param>
        [LoggingSupportFunction]
        public void LogDebug(string message, params object[] args)
        {
            Variation.Current.LogMessage(message, args);
        }

        /// <summary>
        /// Closes the TestLog
        /// </summary>
        /// <remarks>
        /// Closing the TestLog indicates that the test is complete.
        /// You should always close the testlog when done logging.
        /// Failure to close the TestLog could result in the test being logged as not Completed.
        /// After you call Close() you no longer can call any logging API on the TestLog.
        /// </remarks>
        public void Close()
        {
            instance = null;
            Variation.Current.LogResult(resultMapping[Result]);
            Variation.Current.Close();
        }

        #endregion

        #region Private Members

        void VariationClosing(object sender, EventArgs e)
        {
            instance = null;
            // If a TestLog result was specified at some point, we're report it.
            // If, however, one wasn't, we don't want to report Unknown, we want
            // Variation.Close() to complain about no result reported.
            if (LogManager.GetCurrentTestLogResult() != null)
            {
                Variation.Current.LogResult(resultMapping[Result]);
            }
        }

        #endregion

        #region Static Members

        /// <summary>
        /// Gets the Current TestLog (last one created)
        /// </summary>
        /// <value>the current TestLog if one is created, otherwise, null</value>
        /// <remarks>
        /// The Current TestLog is the last TestLog that was created.
        /// When the Current TestLog is closed the Current TestLog is null.
        /// This is usefull when you do not want to pass a reference to the TestLog to several functions.
        /// It is remended that you use the GlobalLog for writing Common Library and utility functions
        /// that want to preform logging as it will log to this if it is avalible.
        /// </remarks>
        public static TestLog Current
        {
            get
            {
                // If instance is null but Variation.Current isn't, it means
                // either Variation is being used, or a TestLog was created on
                // a different process. For legacy interop we want TestLog.Current
                // to return a non-null value in this case. This way if the parent
                // is using the new logging API but some legacy helper code doesn't,
                // it will still work without requiring a rewrite. Ideally we'd like
                // to remove TestLog and port existing tests to use the new logging
                // API, but this is a concession to pragmatic time constraints.
                if (instance == null && Variation.Current != null)
                {
                    instance = new TestLog();
                }
                return instance;
            }

        }
        private static TestLog instance;

        #endregion

        #region IDisposable Members

        /// <summary>
        /// This will close the log on your behalf. Don't use Close & Dispose.
        /// </summary>
        public void Dispose()
        {
            Close();
        }

        #endregion

        #region Private Data

        static Dictionary<TestResult, Result> resultMapping = new Dictionary<TestResult, Result>();

        #endregion
    }    
}

