// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using Microsoft.Test.Logging;
using Microsoft.Test.Discovery;

namespace Microsoft.Test.Layout.TestTypes
{
    /// <summary>
    /// Constructor.
    /// Base Class for Layout Test.
    /// </summary>
    [TestDefaults(DefaultMethodName = "Run", DefaultSubArea="ElementLayout")]
    public class LayoutTest
    {
        private bool createLog = true;

        /// <summary>
        /// Contructor
        /// </summary>
        public LayoutTest()
        { }

        /// <summary>
        /// Create Window for test case.
        /// </summary>
        void CreateWindow()
        {
            // Adding default window size for cases that use LayoutTest.
            window = new Window();
            window.Height = 600;
            window.Width = 800;
            window.Top = 0;
            window.Left = 0;
            window.Show();
        }

        /// <summary>
        /// Close Window at end of test.
        /// </summary>
        void CloseWindow()
        {
            this.window.Close();
        }

        /// <summary>
        /// Window setup steps for test case.
        /// </summary>
        public virtual void WindowSetup()
        { }

        /// <summary>
        /// Final Test Result.
        /// </summary>
        public bool Result;

        /// <summary>
        /// Test Window.
        /// </summary>
        public Window window;

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
        /// Run method for layout tests.
        /// </summary>
        public void Run()
        {
            if (createLog)
            {
                // Create Log.
                TestLog log = null;
               
                if (TestLog.Current == null)
                {
                    log = new TestLog(this.GetType().Name);                    
                }
                else
                    log = TestLog.Current;

                // Call Test Actions.
                ExecuteTestActions(this as LayoutTest, log);

                //log result and close window
                LogTest(this as LayoutTest, log);
                log.Close();
            }
            else
            {
                // Call Test Actions.
                ExecuteTestActions(this as LayoutTest, null);
            }
        }

        /// <summary>
        /// Execture test actions.
        /// </summary>
        /// <param name="activeTest"></param>
        /// <param name="log"></param>
        void ExecuteTestActions(LayoutTest activeTest, TestLog log)
        {            
            GlobalLog.LogStatus("BEGIN LAYOUT TEST");

            try
            {
                if (activeTest is CodeTest)
                    RunCodeTest(activeTest, log);
                if (activeTest is PropertyDumpTest)
                    RunPropertyDumpTest(activeTest, log);
                if (activeTest is VisualScanTest)
                    RunVisualScanTest(activeTest, log);
            }
            catch (Exception ex)
            {
                GlobalLog.LogEvidence(ex);
            }

            GlobalLog.LogStatus("END LAYOUT TEST");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="activeTest"></param>
        /// <param name="log"></param>
        void RunCodeTest(LayoutTest activeTest, TestLog log)
        {
            try
            {
                //create window 
                ((CodeTest)activeTest).CreateWindow();
                CommonFunctionality.FlushDispatcher();

                //load test info..
                ((CodeTest)activeTest).WindowSetup();
                CommonFunctionality.FlushDispatcher();

                //call test actions..
                ((CodeTest)activeTest).TestActions();
                CommonFunctionality.FlushDispatcher();

                ////call verify..
                ((CodeTest)activeTest).TestVerify();
                CommonFunctionality.FlushDispatcher();

                ((CodeTest)activeTest).CloseWindow();
                CommonFunctionality.FlushDispatcher();
            }
            catch (Exception ex)
            {
                GlobalLog.LogEvidence(ex);
                activeTest.Result = false;                
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="activeTest"></param>
        /// <param name="log"></param>
        void RunPropertyDumpTest(LayoutTest activeTest, TestLog log)
        {
            try
            {
                //create window 
                ((PropertyDumpTest)activeTest).CreateWindow();
                CommonFunctionality.FlushDispatcher();

                ((PropertyDumpTest)activeTest).WindowSetup();
                CommonFunctionality.FlushDispatcher();

                ((PropertyDumpTest)activeTest).DumpAndCompare();
                CommonFunctionality.FlushDispatcher();

                ((PropertyDumpTest)activeTest).CloseWindow();
                CommonFunctionality.FlushDispatcher();
            }
            catch (Exception ex)
            {
                GlobalLog.LogEvidence(ex);
                activeTest.Result = false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="activeTest"></param>
        /// <param name="log"></param>
        void RunVisualScanTest(LayoutTest activeTest, TestLog log)
        {
            try
            {
                //create window 
                ((VisualScanTest)activeTest).CreateWindow();
                CommonFunctionality.FlushDispatcher();

                ((VisualScanTest)activeTest).WindowSetup();
                CommonFunctionality.FlushDispatcher();

                ((VisualScanTest)activeTest).CaptureAndCompare();
                CommonFunctionality.FlushDispatcher();

                ((VisualScanTest)activeTest).CloseWindow();
                CommonFunctionality.FlushDispatcher();
            }
            catch (Exception ex)
            {
                GlobalLog.LogEvidence(ex);
                activeTest.Result = false;
            }
        }


        /// <summary>
        /// Logs Test.
        /// </summary>
        /// <param name="activeTest"></param>
        /// <param name="log"></param>
        void LogTest(LayoutTest activeTest, TestLog log)
        {
            if (!activeTest.Result)
            {
                log.LogEvidence(string.Format("[ {0} ] Test Failed.", activeTest.GetType().Name));
                log.Result = TestResult.Fail;
            }
            else
            {
                log.LogEvidence(string.Format("[ {0} ] Test Passed.", activeTest.GetType().Name));
                log.Result = TestResult.Pass;
            }
        }
    }
}
