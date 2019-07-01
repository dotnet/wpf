// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System;
using Microsoft.Test.Logging;

/******************************************************************************
 * 
 * This file contains the Base Framework implementation of a testcase
 * The Testcase class defines a testcase as a series of test steps
 * seperated into Initialize, Run and CleanUp steps that return a 3 state result
 * The class also provides basic Logging functionality
 * 
 *****************************************************************************/

namespace Microsoft.Test.TestTypes
{
    /// <summary>
    /// Delegate for Testcase Steps (Initialize, Run, Validate, CleanUp, etc.)
    /// </summary>
    public delegate TestResult TestStep();

    /// <summary>
    /// Base Class provides a framework for basic Testcase functionality
    /// Defines a Testcase as a series of Run, Initialize, and CleanUp steps
    /// </summary>
    public abstract class StepsTest
    {
        #region Private Data

        private TestLog log;
        private string _testlogname;
        private Type setExpectedErrorType;
        private string exceptionLevel = "Outer";
        private TestResult currentStepResult;
        private bool createLog = true;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the Testcase
        /// </summary>
        protected StepsTest()
        {
            _testlogname = this.GetType().Name;
        }

        #endregion

        #region Public and Protected Members

        /// <summary>
        /// Event containing all of the attached Test Steps used for initialization
        /// </summary>
        public event TestStep InitializeSteps;

        /// <summary>
        /// Event containing all of the attached Test Steps used to perform the test
        /// </summary>
        public event TestStep RunSteps;

        /// <summary>
        /// Event containing all of the attached Test Steps used for CleanUp (these will be invoked in reverse order)
        /// </summary>
        public event TestStep CleanUpSteps;

        /// <summary>
        /// Log that will be used in the test.
        /// </summary>  
        public TestLog Log
        {
            get { return log; }
        }

        /// <summary>
        /// Name passed to the constructor of the log.
        /// </summary>  
        public string TestLogName
        {
            get { return _testlogname; }
            set { _testlogname = value; }
        }

        /// <summary>
        /// Property that determines whether a default log is needed for the test.
        /// This enables old tests to work around a recent infra change that does not allow 
        /// nested TestLogs
        /// </summary>
        public bool CreateLog
        {
            get { return createLog; }
            set { createLog = value; }
        }

        /// <summary>
        /// Runs the Test
        /// </summary>
        public void Run()
        {
            if (!createLog)
            {
                TestResult initializationResult = TestResult.Unknown;
                initializationResult = ExecuteSteps(InitializeSteps, false);

                if (initializationResult == TestResult.Pass || initializationResult == TestResult.Unknown)
                {
                    ExecuteSteps(RunSteps, false);
                }

                OnRunStageComplete();
                ExecuteSteps(CleanUpSteps, true);
            }
            else
            {
                //Runs the Initialize, Run, and CleanUp steps for the test and returns the result
                //***  Initialize the Test  ***
                using (log = new TestLog(this._testlogname))
                {
                    log.LogStatus("Initializing StepsTest");
                    log.Result = ExecuteSteps(InitializeSteps, false);

                    //***  Run the Test  ***
                    if (log.Result == TestResult.Pass || log.Result == TestResult.Unknown)
                    {
                        log.LogStatus("Running StepsTest");
                        log.Result = ExecuteSteps(RunSteps, false);
                    }
                    
                    OnRunStageComplete();

                    //***  Clean Up the Test  ***
                    log.LogStatus("Cleaning up StepsTest");
                    log.Result = ExecuteSteps(CleanUpSteps, true);                    
                }
                //Ensure that this log instance is not used after it is closed.
                log = null;
            }
        }

        /// <summary>
        /// Informs the framework that it is expected for the Type passed as a parameter to
        /// be caught by the try/catch of the current step.
        /// Useful when testing bad code to throw exceptions.
        /// </summary>
        public void SetExpectedErrorTypeInStep(Type t)
        {
            setExpectedErrorType = t;
        }

        /// <summary>
        /// Informs the framework that it is expected for the Type passed as a parameter to
        /// be caught by the try/catch of the current step.
        /// This version takes a string indicating exception level.
        /// </summary>
        public void SetExpectedErrorTypeInStep(Type t, string s)
        {
            setExpectedErrorType = t;
            exceptionLevel = s;
        }

        /// <summary>
        /// Logs the current status of the testcase
        /// </summary>
        /// <param name="status">text to be logged</param>
        /// <remarks>Example:   Status("Connecting to Database");</remarks>
        [LoggingSupportFunction]
        public void Status(string status)
        {
            if (log != null)
            {
                log.LogStatus(status + "...");
            }
        }

        /// <summary>
        /// Logs a Comment for the testcase that will end up in the final Result
        /// logged by the testcase.
        /// </summary>
        /// <param name="comment">text to be logged</param>
        /// <remarks>Example:   LogComment("Database is unavailable");</remarks>
        [LoggingSupportFunction]
        public void LogComment(string comment)
        {
            if (log != null)
            {
                log.LogEvidence(comment);
            }
        }

        /// <summary>
        /// Called after all Run steps are completed, and before we start executing cleanup steps.
        /// Added to make /Pause work and the UI be responsive while pausing
        /// </summary>
        protected virtual void OnRunStageComplete() { }

        /// <summary>
        /// Invokes a Test Step Delegate
        /// </summary>
        /// <param name="step">TestStep to invoke</param>
        /// <returns>the result of the delegate</returns>
        protected virtual TestResult InvokeStep(TestStep step)
        {
            return step();
        }

        #endregion

        #region Internal Members

        /// <summary>
        /// Invokes a Test Step Delegate with exception handling (including dealing with expected exceptions.)
        /// </summary>
        /// <param name="step">TestStep to invoke</param>
        /// <returns>the result of the delegate</returns>
        internal TestResult InvokeStepInternal(TestStep step)
        {
            currentStepResult = TestResult.Unknown;

            Status("Invoking Step " + step.Method.ToString());

            try
            {
                currentStepResult = InvokeStep(step);
                if (setExpectedErrorType != null)
                {
                    LogComment("Expected Error of type " + setExpectedErrorType.FullName + " wasn't thrown!!!");
                    currentStepResult = TestResult.Fail;
                }
            }
            catch (Exception e)
            {
                if (exceptionLevel == "Inner")
                {
                    if (e.InnerException == null)
                    {
                        //Default to Exception e.
                        LogComment("No InnerException exists - verifying the Outer Exception");
                    }
                    else
                    {
                        //Will verify the InnerException, rather than the Exception.
                        e = e.InnerException;
                    }
                }

                if (exceptionLevel != "Outer" && exceptionLevel != "Inner")
                {
                    LogComment("FAIL: Exception Level must be 'Outer' or 'Inner'.");
                    LogComment(e.ToString());
                    currentStepResult = TestResult.Fail;
                }
                else if (setExpectedErrorType == null)
                {
                    LogComment("An unexpected Exception has occured.");
                    LogComment(e.ToString());
                    currentStepResult = TestResult.Fail;
                }
                else if (setExpectedErrorType != e.GetType() && !e.GetType().IsSubclassOf(setExpectedErrorType))
                {
                    LogComment("An incorrect Exception has occured.");
                    LogComment(e.ToString());
                    currentStepResult = TestResult.Fail;
                }
                else
                {
                    Status("The expected Exception Type: " + e.GetType().FullName);
                    Status(e.Message);
                    currentStepResult = TestResult.Pass;
                }
            }
            finally
            {
                Status("Completing   Step: " + step.Method.ToString() + ": " + currentStepResult.ToString());
            }

            setExpectedErrorType = null;

            return currentStepResult;
        }

        #endregion

        #region Private Members

        // TODO: Fix bug in the order of CleanUp steps - only happens when test case writer adds more than
        // one clean up step
        private TestResult ExecuteSteps(TestStep steps, bool reverseOrder)
        {
            if (steps != null)
            {
                if (reverseOrder)
                {
                    for (int i = steps.GetInvocationList().Length - 1; i >= 0; i--)
                    {
                        TestResult result = InvokeStepInternal((TestStep)steps.GetInvocationList()[i]);
                        if (result != TestResult.Pass)
                            return result;
                    }
                }
                else
                {
                    for (int i = 0; i < steps.GetInvocationList().Length; i++)
                    {
                        TestResult result = InvokeStepInternal((TestStep)steps.GetInvocationList()[i]);
                        if (result != TestResult.Pass)
                            return result;
                    }
                }
            }
            return TestResult.Pass;
        }

        #endregion
    }
}
