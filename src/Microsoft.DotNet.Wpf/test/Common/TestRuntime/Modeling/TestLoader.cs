// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml;
using System.Collections;
using System.Security.Permissions;
using System.Runtime.InteropServices;
using System.Threading;

using System.Runtime.Serialization;
using System.IO;
using System.Windows.Interop;
using Microsoft.Test.Win32;

namespace Microsoft.Test.Modeling
{
    ///<summary>
    /// This enqueue the test cases transitions for later executing.  It is implemented
    /// on a Push and Pull Model to get the transitions, the pull model happens when
    /// the executing enters to a nested pump.  The pull model don't happen automatically, 
    /// the user needs to know how request for the next item.
    ///</summary>
    public abstract class TestCaseLoader
    {

        ///<summary>
        /// Constructor
        ///</summary>
        public TestCaseLoader()
        {
            _models = new Hashtable();
            TotalFailures = new ArrayList();
        }



        ///<summary>
        /// Add the needed models to execute the traversals. You need to add the models before to call Run.
        ///</summary>
        public void AddModel(Model model)
        {
            if (model == null)
                throw new ArgumentNullException("AddModel cannot take null as valid parameter");

            if (!_models.ContainsValue(model))
            {
                _models.Add(model.Name, model);
            }
        }


        ///<summary>
        /// Return the hashTable with the models
        ///</summary>
        public Hashtable Models
        {
            get
            {
                return _models;
            }
        }

        ///<summary>
        /// 
        ///</summary>
        public bool ShouldCreateTestLogs
        {
            get
            {
                return _shouldCreateTestLogs;
            }
            set
            {
                _shouldCreateTestLogs = value;
            }
        }

        private bool _shouldCreateTestLogs = true;

        ///<summary>
        /// Execute the traversals for the XTC passed to the constructor.
        /// Load the Xtc file, create the actions queue, and dispatch the actions.
        ///</summary>
        public bool Run()
        {
            LogStatus("* Model loader: Started...");

            // Enqueue all model cases.
            Initialization();

            LogStatus("* Model loader: Executing model cases...");

            int totalCases = _asyncActions.AmountTestCases;

            //VariationContext vc = null;

            if (ShouldCreateTestLogs)
            {
            //    vc = new VariationContext("Modeling");
            }

            // Loop through each case in the queue.
            for (int testCaseIndex = 1; testCaseIndex <= totalCases; testCaseIndex++)
            {
                bool testResult = false;
                bool testLastActionResult = false;
                bool alreadyBegan = false;

                //TestLog testLog = null;

                if (ShouldCreateTestLogs)
                {
                    //testLog = new TestLog(testCaseIndex.ToString());
                }

                LogStatus("* Start model case #" + testCaseIndex.ToString());

                _asyncActions.MoveToNextTestCase();

                ModelTestCase modelTestCase = _asyncActions.CurrentModelTestCase;

                try
                {
                    while (!modelTestCase.IsTestCaseCompleted)
                    {
                        ModelTransition currentAction = modelTestCase.GetModelTransition();

                        if (currentAction == null)
                            throw new InvalidOperationException("ModelTransition is expected");


                        if (!alreadyBegan)
                        {
                            alreadyBegan = true;

                            currentAction.CurrentModel.CurrStartState = currentAction.EndState;
                            testResult = currentAction.CurrentModel.BeginCase(currentAction.EndState);
                        }

                        if (!String.IsNullOrEmpty(currentAction.ActionName))
                        {

                            LogStatus("* Executing Action: " + currentAction.ActionName);


                            testResult = currentAction.CurrentModel.ExecuteAction(currentAction.ActionName,
                                currentAction.EndState, currentAction.InParams, currentAction.OutParams);

                            currentAction.CurrentModel.CurrStartState = currentAction.EndState;

                            if (modelTestCase.IsTestCasePassed == ModelTestCaseResult.Failed)
                            {
                                LogComment("* Action Failed: " + modelTestCase.ActionBeforeTransitionFailure + " Returned False");
                                break;
                            }
                            else if (testResult == false)
                            {
                                LogComment("* Action Failed: " + currentAction.ActionName + " Returned False");
                                break;
                            }
                        }

                    }

                }
                catch (Exception e)
                {
                    LogStatus("Exception occurred");
                    testResult = false;
                    LogComment(e.ToString());
                }
                finally
                {
                    // We need to ensure that EndCase always will be called to clean up any resource that the test case 
                    // is holding on.
                    LogStatus("* End model case: " + testCaseIndex.ToString());
                    ModelTransition lastAction = modelTestCase.GetModelTransition();

                    testLastActionResult = lastAction.CurrentModel.EndCase(lastAction.EndState);
                }

                if (testResult == false || !testLastActionResult || modelTestCase.IsTestCasePassed == ModelTestCaseResult.Failed)
                {
                    LogComment("Model case #" + testCaseIndex.ToString() + " ended with an error");

                    if (ShouldCreateTestLogs)
                    {
                        //testLog.Result = TestResult.Fail;
                    }

                    TotalFailures.Add(testCaseIndex);
                }
                else
                {
                    LogComment("Model case #" + testCaseIndex.ToString() + " completed");

                    if (ShouldCreateTestLogs)
                    {
                        //testLog.Result = TestResult.Pass;
                    }
                }

                if (ShouldCreateTestLogs)
                {
                    //testLog.Close();
                }
            }

            LogStatus("* Model loader: All test cases were executed");

            CleanUpModels();

            LogStatus("* Model loader: Done");

            if (ShouldCreateTestLogs)
            {
                //vc.Close();
            }

            return TotalFailures.Count == 0;
        }


        ///<summary>
        ///
        /// Run on a completed Async mode. Pass true, if you want a async behavior but there is ther no message loop
        /// running.
        ///
        ///</summary>
        public void RunAsync(bool needWin32Loop)
        {
            Initialization();
            _asyncActions.StartAsyncExecution();
            //VariationContext vc = new VariationContext("Modeling");
            if (needWin32Loop)
            {
                MSG msg = new MSG();
                while (NativeMethods.GetMessage(ref  msg, IntPtr.Zero, 0, 0) != 0)
                {
                    NativeMethods.TranslateMessage(ref msg);
                    NativeMethods.DispatchMessage(ref msg);
                    msg = new MSG();
                }
            }
            //vc.Close();

        }

        ///<summary>
        /// 
        /// This event will be raised when the automation is completed
        /// 
        ///</summary>
        public event EventHandler RunCompleted;


        ///<summary>
        /// 
        /// This method will be called during Initialization to populate the ActionQueue
        /// 
        ///</summary>
        protected abstract ActionsQueue CreateActionQueue();


        ///<summary>
        /// 
        /// This method initialize all the models and calls to populate the Test cases ActionQueue
        /// 
        ///</summary>
        public virtual void Initialization()
        {
            if (_models.Count <= 0)
            {
                throw new InvalidOperationException("You need to add models to the traversal before call run");
            }

            IEnumerator models = ((IEnumerable)_models.Values).GetEnumerator();

            LogStatus("* Model loader: " + _models.Count.ToString() + " models loaded.");
            LogStatus("* Model loader: Initializing all loaded models");
            while (models.MoveNext())
            {
                Model model = (Model)models.Current;

                model.Initialize();
            }

            LogStatus("* Model loader: Enqueuing test case transitions");

            //Need to Assert for FileIOPermissions in order for CreateActionQueue() to have
            //access to the test's XTC file in Partial Trust
            FileIOPermission fip = new FileIOPermission(PermissionState.Unrestricted);
            fip.Assert();
            //Call out to the upper class.
            ActionsQueue queue = CreateActionQueue();
            
            LogStatus("* Model loader: Enqueuing test case transitions completed\r\n");

            _asyncActions = new Win32AsyncActionsManager(queue);
            _asyncActions.TestcaseLoader = this;
            models.Reset();

            while (models.MoveNext())
            {
                Model model = (Model)models.Current;
                model.SetAsyncHelper(_asyncActions);
            }
        }


        ///<summary>
        /// 
        /// Cleaning up all the Models
        /// 
        ///</summary>
        internal void CleanUpModels()
        {
            IEnumerator models = ((IEnumerable)_models.Values).GetEnumerator();
            LogStatus("* Model loader: Cleaning up all models");
            models.Reset();
            while (models.MoveNext())
            {
                Model model = (Model)models.Current;
                model.CleanUp();
            }
        }

        ///<summary>
        /// 
        /// This is will be called by Win32AsyncActionManager when we run on Async Model and the 
        /// run it is completed
        /// 
        ///</summary>
        internal void OnRunCompleted()
        {
            if (this.RunCompleted != null)
                this.RunCompleted(this, EventArgs.Empty);
        }


        ///<summary>
        /// Log a result comment.
        ///</summary>
        private void LogComment(string comment)
        {
            //GlobalLog.LogComment(comment);
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

        Hashtable _models = null;
        Win32AsyncActionsManager _asyncActions = null;

        ///<summary>
        /// Array of failures
        ///</summary>
        public ArrayList TotalFailures;
    }


    ///<summary>
    /// This expcetion is throw by the Modeling test case loader.
    ///</summary>
    [Serializable]
    public class ModelLoaderException : Exception
    {
        ///<summary>
        /// Contructor 0 Params
        ///</summary>
        public ModelLoaderException() : base() { }


        ///<summary>
        /// Constructor with String
        ///</summary>
        public ModelLoaderException(string str) : base(str) { }

        ///<summary>
        /// Constructor with String
        ///</summary>
        public ModelLoaderException(string str, Exception exp) : base(str, exp) { }

        /// <summary>
        /// This constructor is required for deserializing this type across AppDomains.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        public ModelLoaderException(SerializationInfo info, StreamingContext context) : base(info, context) { }

    }



    ///<summary>
    /// It is a queue of ModelTransition that it is use on XtcParser class to represent the test cases on a XTC
    ///</summary>
    public class ActionsQueue
    {

        ///<summary>
        /// Constructor for ActionsQueue
        ///</summary>
        public ActionsQueue()
        {
            _queue = new Queue();
        }


        ///<summary>
        /// Amount of test cases on the queue
        ///</summary>
        public int AmountTestCases
        {
            get
            {
                return _queue.Count;
            }

        }

        ///<summary>
        /// Enqueue a modeling action. If an actionTestCaseDelimiter is pass we incress the Amount of test cases (property)
        ///</summary>
        public void Enqueue(ModelTestCase action)
        {

            lock (_queue)
            {
                _queue.Enqueue(action);
            }
        }


        ///<summary>
        /// Current test cases that is been used. If you want to change the current test case you
        /// need to call MoveToNextTestCase
        ///</summary>
        public ModelTestCase CurrentTestCase
        {
            get
            {
                return _modelTestCase;
            }
        }


        ///<summary>
        /// Pop the next Action from the Queue 
        ///</summary>
        public void MoveToNextTestCase()
        {
            lock (_queue)
            {
                _modelTestCase = (ModelTestCase)_queue.Dequeue();
            }
        }


        Queue _queue = null;
        ModelTestCase _modelTestCase = null;

    }




    ///<summary>
    /// This class is use as delimiter on the Queue to separete test cases
    ///</summary>
    public class ModelEndTransition : ModelTransition
    {
        ///<summary>
        /// Constructor
        ///</summary>
        public ModelEndTransition(Model model, State endState)
        {
            base.model = model;
            base.EndStateInternal = endState;
        }
    }


    ///<summary>
    /// This represent an Trasition Total State on Modeling
    ///</summary>
    public class ModelTransition
    {
        /// <summary>
        /// 
        /// </summary>
        protected ModelTransition() { }


        /// <summary>
        /// 
        /// </summary>
        public ModelTransition(Model currModel)
            : this(currModel, "",
            new State(), new State(), new State(), new State()) { }


        /// <summary>
        /// 
        /// </summary>
        public ModelTransition(Model currModel, string actionName, State currentState, State endState, State objInParams, State objOutParams)
            : base()
        {
            this.model = currModel;
            _actionName = actionName;
            this.EndStateInternal = endState;
            _objInParams = objInParams;
            _objOutParams = objOutParams;
            _currentState = currentState;
        }

        /// <summary>
        /// Current Model for the Action
        /// </summary>
        public Model CurrentModel
        {
            get
            {
                return model;
            }
        }

        /// <summary>
        /// Action Name on this state
        /// </summary>
        public string ActionName
        {
            get
            {
                return _actionName;
            }
            set
            {
                _actionName = value;
            }
        }

        /// <summary>
        /// The end state for the action
        /// </summary>
        public State EndState
        {
            get
            {
                return EndStateInternal;
            }
        }

        /// <summary>
        /// In Params passed on the Action
        /// </summary>
        public State InParams
        {
            get
            {
                return _objInParams;
            }
        }

        /// <summary>
        ///    Out Params on the Actions
        /// </summary>
        public State OutParams
        {
            get
            {
                return _objOutParams;
            }
        }

        /// <summary>
        /// Current State
        /// </summary>
        public State CurrentState
        {
            get
            {
                return _currentState;
            }
        }

        /// <summary>
        /// Model that this action is related
        /// </summary>
        protected Model model;
        string _actionName;

        /// <summary>
        /// End state
        /// </summary>
        protected State EndStateInternal;

        State _objInParams, _objOutParams, _currentState;

    }


}
