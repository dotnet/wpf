// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System;
using System.Windows.Threading;
using Microsoft.Test.Logging;
using Microsoft.Test.Modeling;
using Microsoft.Test.Threading;

/******************************************************************************
 *  
 * This file contains the base class of any model testcase.
 * The .xtc file with the state machine should be created using MDE.
 * 
 *****************************************************************************/

namespace Microsoft.Test.TestTypes
{
    /// <summary>
    /// Base class for using model-based testing to test Avalon.
    /// </summary>
    public abstract class AvalonModel : Model
    {
        #region Private Data

        private string xtcFileName;
        private XtcTestCaseLoader loader;
        private TestLog log;
        private string testLogName;
        private int firstCase; 
        private int lastCase;
        private DispatcherSignalHelper dispatcherSignalHelper;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor for AvalonModel.
        /// If firstCase and lastCase are both set to -1 all tests in the xtc file will be run.
        /// To run only one test, set them both to its number.
        /// <param name="modelName">Name of the model. If no name is specified, the .xtc file name 
        /// will be used as the model name.</param>
        /// <param name="xtcFileName">Name of xtc file.</param>
        /// <param name="firstCase">Number of first test case to run.</param>
        /// <param name="lastCase">Number of last test case to run.</param>
        /// </summary>
        protected AvalonModel(string modelName, string xtcFileName, int firstCase, int lastCase)
        {
            this.xtcFileName = xtcFileName;
            if (modelName == "")
            {
                //If no model name specified, use the name of the .xtc file as the model name.
                this.Name = this.GetType().Name;
            }
            else
            {
                this.Name = modelName;
            }
            this.firstCase = firstCase;
            this.lastCase = lastCase;
            testLogName = this.Name;
            dispatcherSignalHelper = new DispatcherSignalHelper();
        }

        #endregion

        #region Public and Protected Members

        /// <summary>
        /// Name used to create a new TestLog.
        /// </summary>  
        public string TestLogName
        {
            get { return testLogName; }
            set { testLogName = value; }
        }

        /// <summary>
        /// Test log used in this test.
        /// </summary>  
        protected TestLog Log
        {
            get { return log; }
            set { log = value; }
        }

        /// <summary>
        /// Runs the XtcTestCaseLoader with the model added.
        /// </summary>
        public TestLog Run()
        {
            using (log = new TestLog(this.testLogName))
            {
                log.Result = RunLoader();
            }
            return log;
        }

        /// <summary>
        /// Logs the current status of the testcase
        /// </summary>
        /// <param name="status">text to be logged</param>
        /// <remarks>Example:   Status("Connecting to Database");</remarks>
        [LoggingSupportFunction]
        public void Status(string status)
        {
            log.LogStatus(status + "...");
        }

        /// <summary>
        /// Waits for an asynchronous event to send a signal (default timeout 30 sec)
        /// </summary>
        /// <returns>
        /// Result.Unknown if a signal was not sent in the timeout period,
        /// otherwise, the Result set by the Signal()
        /// </returns>
        public TestResult WaitForSignal()
        {
            return dispatcherSignalHelper.WaitForSignal();
        }

        /// <summary>
        /// Waits for an asynchronous event to send a signal (default timeout 30 sec)
        /// </summary>
        /// <param name="name">Name of the signal</param>
        /// <returns>
        /// Result.Unknown if a signal was not sent in the timeout period,
        /// otherwise, the Result set by the Signal()
        /// </returns>
        public TestResult WaitForSignal(string name)
        {
            return dispatcherSignalHelper.WaitForSignal(name);
        }

        /// <summary>
        /// Waits for an asynchronous event to send a signal
        /// </summary>
        /// <param name="timeout">number of milliseconds to wait for the signal before timing-out</param>
        /// <returns>
        /// Result.Unknown if a signal was not sent in the timeout period,
        /// otherwise, the Result set by the Signal()
        /// </returns>
        public TestResult WaitForSignal(int timeout)
        {
            return dispatcherSignalHelper.WaitForSignal(timeout);
        }

        /// <summary>
        /// Waits for an asynchronous event to send a signal
        /// </summary>
        /// <param name="name">Name of the signal</param>
        /// <param name="timeout">number of milliseconds to wait for the signal before timing-out</param>
        /// <returns>
        /// Result.Unknown if a signal was not sent in the timeout period,
        /// otherwise, the Result set by the Signal()
        /// </returns>
        public TestResult WaitForSignal(string name, int timeout)
        {
            return dispatcherSignalHelper.WaitForSignal(name, timeout);
        }

        /// <summary>
        /// Signals a result back to the WaitForSignal() method
        /// </summary>
        /// <param name="result">the result to return from the TestStep</param>
        public void Signal(TestResult result)
        {
            dispatcherSignalHelper.Signal(result);
        }

        /// <summary>
        /// Signals a result back to the WaitForSignal() method
        /// </summary>
        /// <param name="name">Name of the signal</param>
        /// <param name="result">the result to return from the TestStep</param>
        public void Signal(string name, TestResult result)
        {
            dispatcherSignalHelper.Signal(name, result);
        }

        /// <summary>
        /// Waits for a specific Dispatcher Priority to occur
        /// </summary>
        /// <param name="priority">Dispatcher Priority to wait for</param>
        /// <returns>true if sucessfull otherwise false when a timeout occurs</returns>
        public void WaitForPriority(DispatcherPriority priority)
        {
            DispatcherHelper.DoEvents(0, priority);
        }

        /// <summary>
        /// Processes events from the dispatcher event queue for a specified number of milliseconds.
        /// </summary>
        /// <param name="milliseconds">number of milliseconds to process events for</param>
        public void WaitFor(int milliseconds)
        {
            DispatcherHelper.DoEvents(milliseconds);
        }

        /// <summary>
        /// Delegate that is expected to throw an exception, when doing negative testing.
        /// </summary>
        protected delegate void ExceptionDelegate();

        /// <summary>
        /// Used in negative testing, to make sure a certain expected exception happens.
        /// Added because SetExpectedErrorTypeInStep exists in Testcase, from which model test cases
        /// do not derive from. 
        /// </summary>
        /// <param name="exceptionType">The type of the exception.</param>
        /// <param name="exceptionDelegate">Delegate that is expected to throw the exception.</param>
        /// <returns>True if the exception was thrown, false if it wasn't.</returns>
        protected bool ExpectException(string exceptionType, ExceptionDelegate exceptionDelegate)
        {
            bool exceptionCaught = false;
            try
            {
                exceptionDelegate();
            }
            catch (Exception e)
            {
                if (e.GetType().ToString() == exceptionType)
                {
                    GlobalLog.LogStatus(exceptionType.ToString() + " caught as expected");
                    exceptionCaught = true;
                }
            }
            if (!exceptionCaught)
            {
                GlobalLog.LogStatus("Fail - " + exceptionType.ToString() + " not thrown");
                return false;
            }
            return true;
        }

        #endregion

        #region Overridden Members

        /// <summary>
        /// This method was overriden to keep track of the current log result. This will be needed when 
        /// invoking the test case with /pause.
        /// </summary>
        public override bool ExecuteAction(string action, State state, State inParams, State outParams)
        {
            bool resultExecuteAction = base.ExecuteAction(action, state, inParams, outParams);
            // If we don't set Log.Result here, when Pausing a test case the result will always be Unknown
            this.Log.Result = (resultExecuteAction ? TestResult.Pass : TestResult.Fail);
            return resultExecuteAction;
        }

        /// <summary>
        /// This method was overriden to keep track of the current log result. This will be needed when 
        /// invoking the test case with /pause.
        /// </summary>
        public override bool ExecuteAction(string action, State state, State inParams)
        {
            bool resultExecuteAction = base.ExecuteAction(action, state, inParams);
            // If we don't set Log.Result here, when Pausing a test case the result will always be Unknown
            this.Log.Result = (resultExecuteAction ? TestResult.Pass : TestResult.Fail);
            return resultExecuteAction;
        }

        /// <summary>
        /// This method was overriden to keep track of the current log result. This will be needed when 
        /// invoking the test case with /pause.
        /// </summary>
        public override bool ExecuteAction(string action, State state)
        {
            bool resultExecuteAction = base.ExecuteAction(action, state);
            // If we don't set Log.Result here, when Pausing a test case the result will always be Unknown
            this.Log.Result = (resultExecuteAction ? TestResult.Pass : TestResult.Fail);
            return resultExecuteAction;
        }

        /// <summary>
        /// This method was overriden to keep track of the current log result. This will be needed when 
        /// invoking the test case with /pause.
        /// </summary>
        public override bool ExecuteAction(string action)
        {
            bool resultExecuteAction = base.ExecuteAction(action);
            // If we don't set Log.Result here, when Pausing a test case the result will always be Unknown
            this.Log.Result = (resultExecuteAction ? TestResult.Pass : TestResult.Fail);
            return resultExecuteAction;
        }

        /// <summary/>
        public override bool CleanUp()
        {
            return base.CleanUp();
        }

        #endregion

        #region Private Members

        private TestResult RunLoader()
        {
            if (firstCase == -1 && lastCase == -1)
            {
                // Run all test cases in this xtc file
                loader = new XtcTestCaseLoader(xtcFileName);
            }
            else
            {
                loader = new XtcTestCaseLoader(xtcFileName, firstCase, lastCase);
            }
            loader.AddModel(this);
            loader.ShouldCreateTestLogs = false;
            if (loader.Run())
            {
                return TestResult.Pass;
            }
            return TestResult.Fail;
        }

        #endregion
    }
}
