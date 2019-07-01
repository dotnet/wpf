// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Test.Win32;
using System.Runtime.InteropServices;

namespace Microsoft.Test.Modeling
{


    ///<summary>
    /// Async Actions Manager to execute async actions on Modeling.
    ///</summary>
    public abstract class AsyncActionsManager : MarshalByRefObject
    {
        ///<summary>
        /// 
        ///</summary>
        public abstract void ExecuteAsyncNextAction();
    }



    ///<summary>
    /// This class uses a Win32 Message Only Window to provide Async execution behavior
    /// for ITE or TMT xtc models
    ///</summary>
    public class Win32AsyncActionsManager : AsyncActionsManager
    {

        ///<summary>
        /// Construtor that takes a ActionQueue that it will be used to pull all the action 
        /// that are going to be executed
        ///</summary>
        internal Win32AsyncActionsManager(ActionsQueue queue)
        {

            if (queue == null)
                throw new ArgumentNullException("the ActionQueue cannot be null");

            if (queue.AmountTestCases <= 0)
                throw new InvalidOperationException("The queue doesn't have any items");

            _queue = queue;
            _hwnd = new HwndWrapper(0, 0, 0, 0, 0, 0, 0, "", NativeConstants.HWND_MESSAGE, null);
            _hwndWrapperHook = new HwndWrapperHook(_hwndHook);
            _hwnd.AddHook(_hwndWrapperHook);
        }

        ///<summary>
        /// Binding a XTCLoader to the Class, The only need to this is for Async behavior.
        /// Sync behavior doesn't need this.
        ///</summary>
        internal TestCaseLoader TestcaseLoader
        {
            set
            {
                if (value == null)
                    throw new ArgumentNullException("The XtcLoader cannot be null");

                _testcaseLoader = value;
            }
        }


        ///<summary>
        /// Amount of test cases that are still need to be processed
        ///</summary>
        internal int AmountTestCases
        {
            get
            {
                return _queue.AmountTestCases;
            }

        }


        ///<summary>
        /// Post a message to the message only window to execute the next Action on the queue
        ///</summary>
        override public void ExecuteAsyncNextAction()
        {
            NativeMethods.PostMessage(new HandleRef(null, _hwnd.Handle), ActionwindowMessage, IntPtr.Zero, IntPtr.Zero);
        }


        ///<summary>
        /// Start
        ///</summary>
        internal void StartAsyncExecution()
        {
            NativeMethods.PostMessage(new HandleRef(null, _hwnd.Handle), StartAsyncCaseExecution, IntPtr.Zero, IntPtr.Zero);
        }


        ///<summary>
        /// Pop the next Action from the Queue 
        ///</summary>
        internal void MoveToNextTestCase()
        {
            _queue.MoveToNextTestCase();
        }


        ///<summary>
        /// Gets the current test case
        ///</summary>
        internal ModelTestCase CurrentModelTestCase
        {
            get
            {

                return _queue.CurrentTestCase;
            }
        }


        private IntPtr _hwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            handled = false;

            if (hwnd == _hwnd.Handle && ActionwindowMessage == msg)
            {
                lock (_queue)
                {
                    if (!_ignoreUntilStart || !_runningAsync)
                    {


                        ModelTestCase modelTestCase = _queue.CurrentTestCase;

                        if (modelTestCase != null && modelTestCase.AmountTransitions > 0)
                        {



                            if (modelTestCase.IsTestCaseCompleted)
                            {
                                ModelTransition currentAction = modelTestCase.PeekModelTransition();
                                currentAction.CurrentModel.EndCaseOnNestedPump();
                                if (_runningAsync)
                                {
                                    NativeMethods.PostMessage(new HandleRef(null, _hwnd.Handle), EndAsyncCaseExecution, IntPtr.Zero, IntPtr.Zero);
                                }
                            }
                            else
                            {
                                bool isTransitionPassed = false;

                                ModelTransition currentAction = modelTestCase.GetModelTransition();
                                handled = true;
                                if (currentAction != null)
                                {
                                    LogStatus("* ITE Executing " + currentAction.ActionName);

                                    try
                                    {
                                        isTransitionPassed = currentAction.CurrentModel.ExecuteAction(currentAction.ActionName, currentAction.EndState, currentAction.InParams, currentAction.OutParams);

                                    }
                                    catch (Exception e)
                                    {
                                        isTransitionPassed = false;
                                        LogComment("Expection was caught");
                                        LogComment("Message:");
                                        LogComment(e.Message.ToString());
                                        LogComment("CallStack:");
                                        LogComment(e.StackTrace.ToString());
                                    }
                                    finally
                                    {
                                        currentAction.CurrentModel.CurrStartState = currentAction.EndState;
                                    }

                                    if (!isTransitionPassed)
                                    {
                                        modelTestCase.IsTestCasePassed = ModelTestCaseResult.Failed;
                                        modelTestCase.ActionBeforeTransitionFailure = String.Copy(currentAction.ActionName);
                                        modelTestCase.ClearTransitions();
                                    }

                                }
                                else
                                {
                                    throw new ModelLoaderException("Error: GetActionIntemState retrieve null");
                                }
                            }

                        }
                        else
                        {
                            throw new ModelLoaderException("Error: Trying to dispatch an empty transition from a queue");
                        }
                    }
                }
            }


            if (hwnd == _hwnd.Handle && StartAsyncCaseExecution == msg)
            {
                _ignoreUntilStart = false;
                _runningAsync = true;
                _caseOrderCount++;
                //TestLog testLog = new TestLog(_caseOrderCount.ToString());

                LogComment("**********************");
                LogComment("Executing TEST CASE # " + _caseOrderCount.ToString());

                if (this.AmountTestCases > 0)
                {
                    this.MoveToNextTestCase();
                    ModelTestCase testCase = this.CurrentModelTestCase;

                    ModelTransition currentAction = testCase.GetModelTransition();

                    if (currentAction.ActionName == String.Empty)
                    {
                        currentAction.CurrentModel.CurrStartState = currentAction.EndState;
                        currentAction.CurrentModel.BeginCase(currentAction.EndState);
                    }
                    else
                    {
                        throw new ModelLoaderException("Expecting a empty ActionName. The Testcase is init Async.");
                    }

                }
            }


            if (hwnd == _hwnd.Handle && EndAsyncCaseExecution == msg)
            {
                _ignoreUntilStart = true;

                LogStatus("* ITE End Case: " + _caseOrderCount.ToString());


                ModelTestCase testCase = this.CurrentModelTestCase;

                ModelTransition lastAction = testCase.GetModelTransition();
                lastAction.CurrentModel.EndCase(lastAction.EndState);

                if (testCase.IsTestCasePassed == ModelTestCaseResult.Failed)
                {
                    LogComment("* ITE Action Failed: " + testCase.ActionBeforeTransitionFailure + " Returned False");
                    LogComment("TEST CASE # " + _caseOrderCount.ToString() + " Ended with an Error");
                    //TestLog.Current.Result = TestResult.Fail;
                }
                else
                {
                    LogComment("TEST CASE # " + _caseOrderCount.ToString() + " Completed");
                    //TestLog.Current.Result = TestResult.Pass;
                }


                LogComment("**********************");
                //TestLog.Current.Close();


                if (this.AmountTestCases > 0)
                {
                    NativeMethods.PostMessage(new HandleRef(null, _hwnd.Handle), StartAsyncCaseExecution, IntPtr.Zero, IntPtr.Zero);
                }
                else
                {
                    LogComment("All test cases were executed");
                    LogComment("**************************************************************");

                    _testcaseLoader.CleanUpModels();

                    LogComment("XTCTestCaseLoader ended...");
                    _testcaseLoader.OnRunCompleted();
                }

            }

            return IntPtr.Zero;
        }



        ///<summary>
        /// Log a comment.
        ///</summary>
        private void LogComment(string comment)
        {

            //GlobalLog.LogStatus(comment);
            Console.WriteLine(comment);
        }

        ///<summary>
        /// Log a comment.
        ///</summary>
        private void LogStatus(string comment)
        {

            //GlobalLog.LogStatus(comment);
            Console.WriteLine(comment);
        }

        ActionsQueue _queue = null;
        static int ActionwindowMessage = NativeMethods.RegisterWindowMessage("ModelingActionMessage");
        static int StartAsyncCaseExecution = NativeMethods.RegisterWindowMessage("StartAsyncCaseExecution");
        static int EndAsyncCaseExecution = NativeMethods.RegisterWindowMessage("EndAsyncCaseExecution");
        HwndWrapper _hwnd = null;
        int _caseOrderCount = 0;
        bool _runningAsync = false;
        TestCaseLoader _testcaseLoader = null;
        bool _ignoreUntilStart = false;

        HwndWrapperHook _hwndWrapperHook;

    }

}
