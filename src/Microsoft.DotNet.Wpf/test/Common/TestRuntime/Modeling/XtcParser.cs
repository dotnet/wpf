// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml;
using System.Collections;
using System.Security.Permissions;
using System.IO;
using System.Security;

namespace Microsoft.Test.Modeling
{

    /// <summary>
    /// Represent a model test case that contains a queue with all the transitions for that specific test case
    /// </summary>
    public class ModelTestCase : MarshalByRefObject
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public ModelTestCase()
        {
            _transitionQueue = new Queue();
        }

        /// <summary>
        /// Dequeue 1 transtion from the queue of Transitions
        /// </summary>
        public ModelTransition GetModelTransition()
        {
            return (ModelTransition)_transitionQueue.Dequeue();
        }


        /// <summary>
        /// Peeks to top transition on the queue
        /// </summary>
        public ModelTransition PeekModelTransition()
        {
            return (ModelTransition)_transitionQueue.Peek();
        }


        /// <summary>
        /// Enqueue 1 transtion to the queue of Transitions
        /// </summary>
        public void AddTransition(ModelTransition o)
        {
            //This validates the last item on the queue is a ModelEndTransition

            if (o != null && !_isQueueSealed)
            {
                if (o is ModelEndTransition)
                {
                    _isQueueSealed = true;
                }

                _transitionQueue.Enqueue(o);
            }
        }


        /// <summary>
        /// Empty the Transitions Queue except for the last ModelEndTransition. In 
        /// that way we stop doing more transitions
        /// </summary>
        public void ClearTransitions()
        {
            int count = _transitionQueue.Count;

            //Removing all transition except the last one. The last item should be a ModelEndTransition

            for (int i = 0; i < count - 1; i++)
                _transitionQueue.Dequeue();
        }


        /// <summary>
        /// Total amount of remaining transitions at the current moment
        /// </summary>
        public int AmountTransitions
        {
            get
            {
                return _transitionQueue.Count;
            }
        }

        /// <summary>
        /// Return true if next item on the transition queue is Test case end transition
        /// </summary>
        public bool IsTestCaseCompleted
        {
            get
            {
                return _transitionQueue.Peek() is ModelEndTransition;
            }
        }


        /// <summary>
        /// Used on Async behavior, we store if any action failed
        /// </summary>
        public ModelTestCaseResult IsTestCasePassed
        {
            get
            {
                return _testCaseResult;
            }
            set
            {
                _testCaseResult = value;
            }
        }


        /// <summary>
        /// Store the action name where the result was a failure.
        /// </summary>
        public string ActionBeforeTransitionFailure
        {
            get
            {
                return _actionBeforeTransitionFailure;
            }
            set
            {
                _actionBeforeTransitionFailure = value;
            }
        }

        ModelTestCaseResult _testCaseResult = ModelTestCaseResult.UnSet;
        string _actionBeforeTransitionFailure = String.Empty;
        bool _isQueueSealed = false;
        Queue _transitionQueue = null;

    }

    /// <summary>
    /// 
    /// </summary>
    public enum ModelTestCaseResult
    {
        /// <summary>
        /// 
        /// </summary>
        UnSet,

        /// <summary>
        /// 
        /// </summary>
        Passed,

        /// <summary>
        /// 
        /// </summary>
        Failed
    }


    /// <summary>
    /// Parses a Modeling (ITE or TMT) xtc file and creates a queue with all the transitions
    /// </summary>
    public class XtcParser
    {
        
        /// <summary>
        /// Constructor that it will extract all the test cases on the XTC
        /// </summary>
        public XtcParser(string xtc, Hashtable models) : this(xtc, models, -1, -1) { }


        /// <summary>
        /// Constructor that it will extract the range of cases specify on the firstCase and lastCase params
        /// </summary>
        public XtcParser(string xtc, Hashtable models, int firstCase, int lastCase)
        {
            string xtcAux = xtc;

            if (xtc.IndexOf("<XTC") == -1)
            {
                _xtcFilename = xtc;
                xtcAux = null;
            }

            if (xtc == null)
                throw new ArgumentNullException("The xtc cannot be null. Either XtcFile or XtcContent");

            if (models == null)
                throw new ArgumentNullException("Model parameter cannot be null");

            if (firstCase > 0)
            {
                _firstCase = firstCase;
            }

            if (lastCase >= 1)
            {
                _lastCase = lastCase;
            }
          
            _models = models;

            Initialize(xtcAux);
        }


        private void Initialize(string xtcContent)
        {

            _queue = new ActionsQueue();
            _xmlDoc = new XmlDocument();

            XmlTextReader xmlTR = null;
            StringReader sReader = null;
            if (xtcContent == null)
            {
                xmlTR = new XmlTextReader(_xtcFilename);
            }
            else
            {
                sReader = new StringReader(xtcContent);
                xmlTR = new XmlTextReader(sReader);
            }

            XmlReaderSettings settings = new XmlReaderSettings();
            settings.ValidationType = ValidationType.Schema;
            XmlReader reader = XmlReader.Create(xmlTR, settings);
            string dir = Directory.GetCurrentDirectory();

            FileIOPermission p = new FileIOPermission(FileIOPermissionAccess.Read, dir);

            p.Assert();
            _xmlDoc.Load(reader);
            CodeAccessPermission.RevertAll();

            _nsmgr = new XmlNamespaceManager(_xmlDoc.NameTable);
            _nsmgr.AddNamespace("curr", _xmlDoc.DocumentElement.NamespaceURI);


            _tests = _xmlDoc.DocumentElement.ChildNodes;

            if (_lastCase == -1)
                _lastCase = _tests.Count;

            _prefix = "curr";

        }



        /// <summary>
        /// Extract all the test cases specify on the constructor and return will all the cases
        /// </summary>
        public ActionsQueue CreateActionsQueue()
        {

            int currentCase;

            LogComment("**************************************************************");
            LogComment("Start retrieving all the test cases from the XTC File");
            LogComment((_lastCase - _firstCase + 1).ToString() + " test cases found");

            //Go through each case specified
            for (currentCase = _firstCase; currentCase <= _lastCase; currentCase++)
            {
                LogStatus("Transitions for test case # " + (currentCase).ToString());

                XmlNodeList currentStepsNodeList = SetCurrentStepsForTestIndex(currentCase);



                for (int iStep = 0; iStep < currentStepsNodeList.Count; iStep++)
                {

                    ModelTestCase testCase;
                    testCase = new ModelTestCase();
                    State lastEndState = null;

                    XmlNodeList currentTransitionsList = SetCurrentTransitionFromStep(currentStepsNodeList, iStep);
                    Model lastModel = null;
                    LogStatus((currentTransitionsList.Count).ToString() + " transtions found for current test case");

                    for (int currentTransitionIndex = 0; currentTransitionIndex < currentTransitionsList.Count; currentTransitionIndex++)
                    {
                        lastModel = EnqueueCurrentTransition(currentTransitionsList, currentTransitionIndex, out lastEndState, testCase);
                    }


                    testCase.AddTransition(new ModelEndTransition(lastModel, lastEndState));

                    _queue.Enqueue(testCase);

                    LogStatus("The transitions for test case # " + (currentCase).ToString() + " are retrieved");
                }

            }

            LogComment("All information is enqueued and ready for execution");
            LogComment("**************************************************************");

            return _queue;
        }

        /// <summary>
        /// Executes the Transition that is set by member variables
        /// </summary>
        /// <remarks>Check the Errors collection for errors</remarks>
        /// <returns>false if errors occur</returns>
        private Model EnqueueCurrentTransition(XmlNodeList currentTransitionsList, int transitionIndex, out State lastEndState, ModelTestCase testCase)
        {
            XmlNode actionNode, stateNode;
            string actionName;
            XmlNodeList inParams, outParams;

            State objInParams = null, objOutParams = null;
            State endState = null;

            XmlNode currTransition = null;
            Model currModel;

            //Get the transition Node
            currTransition = currentTransitionsList.Item(transitionIndex);

            //Find the model
            currModel = GetModel(currTransition);

            //Get the In action Params
            objInParams = null;
            inParams = currTransition.SelectNodes("curr:PARAM[@Type = 'In']", _nsmgr);
            if (inParams.Count > 0)
            {
                objInParams = this.GetActionParams(inParams);
                if (objInParams == null)
                {
                    throw new ModelLoaderException("Failure getting Action IN Params");
                }
            }

            //Get the Out action Params
            objOutParams = null;
            outParams = currTransition.SelectNodes("curr:PARAM[@Type = 'Out']", _nsmgr);
            if (outParams.Count > 0)
            {
                objOutParams = this.GetActionParams(outParams);
                if (objOutParams == null)
                {
                    throw new ModelLoaderException("Failure getting Action OUT Params");
                }
            }

            //Get the action Node & use it to get the action name
            actionNode = currTransition.SelectSingleNode("curr:ACTION", _nsmgr);
            int fileID = -1;
            actionName = this.GetAction(actionNode, out fileID);
            if (actionName == null)
            {
                throw new ModelLoaderException("Failure finding an action for this transition");
            }

            //Get the state Node to create the object
            endState = null;
            stateNode = currTransition.SelectSingleNode("curr:STATE", _nsmgr);
            //In stateless model case there will not be a STATE node in the xtc
            if (stateNode != null)
            {
                endState = this.GetState(stateNode);
                if (endState == null)
                {
                    throw new ModelLoaderException("Failure finding the state variables for this transition");
                }
            }


            if ((actionName == null) || (actionName.Trim() == "") && transitionIndex == 0)
            {
                testCase.AddTransition(new ModelTransition(currModel, String.Empty, null, endState, null, null));
                //_queue.Enqueue(new ModelTransition(currModel,String.Empty,null,endState,null,null));
            }
            else
            {
                //Log transition information & execute the action
                LogStatus(currModel.GetType().Name + "." + actionName + "(" + objInParams + ")");
                //_queue.Enqueue(new ModelTransition(currModel, actionName,null,endState,objInParams,objOutParams));
                testCase.AddTransition(new ModelTransition(currModel, actionName, null, endState, objInParams, objOutParams));
            }

            lastEndState = endState;

            return currModel;
        }



        /// <summary>
        /// Returns the model object that is specified in the Transition node
        /// </summary>
        /// <param name="xmlTransitionNode">The XmlNode of the Transition to get the model from</param>
        /// <returns>Model object, null if failure</returns>
        private Model GetModel(XmlNode xmlTransitionNode)
        {

            //Verify required param
            if (xmlTransitionNode == null)
                throw new ModelLoaderException("No Transition node given");


            //Get the attributes for the transition node
            XmlAttributeCollection transAttribs = xmlTransitionNode.Attributes;
            if (transAttribs == null || transAttribs.Count < 1)
                throw new ModelLoaderException("No Attributes found for the given Transition node");


            //Find the attribute ModelName
            string modelName = transAttribs["ModelName"].Value;
            if (modelName == "" || modelName == String.Empty)
                throw new ModelLoaderException("No ModelName attribute found for the given Transition node");

            string modelID = String.Empty;
            int instance = 0;

            //Find the [optional] attribute ModelId
            if (transAttribs["ModelId"] != null)
            {
                modelID = transAttribs["ModelId"].Value;


                if (modelID != "" && modelName != String.Empty)
                {
                    instance = Int32.Parse(modelID);
                }
            }

            Model model = _models[modelName] as Model;

            if (model == null || model.Instance != instance)
                throw new ModelLoaderException("The model was not added to the Traversal");

            return model;
        }



        /// <summary>
        /// Sets the current step (global variables) to the requested step
        /// </summary>
        private XmlNodeList SetCurrentTransitionFromStep(XmlNodeList currentStepsNodeList, int stepIndex)
        {

            //Get the current step node to find transitions for
            XmlElement stepNode = currentStepsNodeList.Item(stepIndex) as XmlElement;
            if (stepNode == null)
                throw new ModelLoaderException("Case " + stepIndex.ToString() + " does not have a STEP node in the XTC.");


            //Get the transitions for the current case (including the start states)
            XmlNodeList transitions = stepNode.SelectNodes("curr:TRANSITION[curr:ACTION]", _nsmgr);

            if (transitions == null || transitions.Count == 0)
                throw new ModelLoaderException("The xml transtions is empty");


            return transitions;
        }


        /// <summary>
        /// Return the Steps for a specific test index
        /// </summary>
        /// <param name="testIndex">The index of the test to make current</param>
        /// <returns></returns>
        private XmlNodeList SetCurrentStepsForTestIndex(int testIndex)
        {
            XmlNodeList nodes = null;

            //LogComment("Making TEST '" + testIndex + "' current");


            //Get the requested case
            XmlElement currTest = _tests.Item(testIndex - 1) as XmlElement;
            if (currTest == null)
                throw new ModelLoaderException("Case " + testIndex.ToString() + " does not exist in this Traversal.");

            //LogComment("Case " + testIndex.ToString());

            //Get the Step(s) for the current case
            nodes = currTest.SelectNodes("curr:STEP", _nsmgr);

            if (nodes == null || nodes.Count == 0)
                throw new ModelLoaderException("Missing XML on the testcase number: " + testIndex.ToString());

            return nodes;
        }







        /// <summary>
        /// Returns a state object from the specified STATE xml string
        /// </summary>
        /// <param name="xmlStateString">the xml string for the state</param>
        /// <returns>State object, null if failure</returns>
        private State GetState(string xmlStateString)
        {
            if (xmlStateString == null || xmlStateString == "")
            {
                LogComment("No State xml string given", "Missing Param");
                return null;
            }

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlStateString);

            XmlNodeList states = xmlDoc.GetElementsByTagName("STATE");
            if (states == null || states.Count < 1)
            {
                LogComment("No State node found in the Xml string", "XTC Parse");
                return null;
            }

            XmlNode stateNode = states.Item(0);

            return GetState(stateNode);
        }

        /// <summary>
        /// Returns a state object from the specified STATE node
        /// </summary>
        /// <param name="xmlStateNode">the XmlNode for the state</param>
        /// <returns>State object, null if failure</returns>
        private State GetState(XmlNode xmlStateNode)
        {
            State stateObject = new State();

            if (xmlStateNode == null)
            {
                LogComment("No State node given", "Missing Param");
                stateObject = null;
            }
            else
            {
                //Get the STATEVAR nodes for the State
                XmlNodeList stateVars;
                if (_nsmgr == null)
                    stateVars = xmlStateNode.SelectNodes("STATEVAR");
                else
                    stateVars = xmlStateNode.SelectNodes(_prefix + ":STATEVAR", _nsmgr);

                //Define variables foreach use in the foreach loop
                XmlAttributeCollection stateVarAttribs;
                XmlNode stateVarNode, stateVarNameAttribNode, stateVarValueAttribNode;
                string stateName, stateValue;

                for (int iVar = 0; iVar < stateVars.Count; iVar++)
                {
                    //Get the individual state variables
                    stateVarNode = stateVars.Item(iVar);

                    //Get the attributes for the state variable
                    stateVarAttribs = stateVarNode.Attributes;
                    if (stateVarAttribs == null || stateVarAttribs.Count < 1)
                    {
                        LogComment("No Attributes found for the given state variable node", "XTC Parse");
                        stateObject = null;
                    }
                    else
                    {
                        //Get the Name attribute
                        stateVarNameAttribNode = stateVarAttribs.GetNamedItem("Name");
                        if (stateVarNameAttribNode == null)
                        {
                            LogComment("No Name attribute found for the given state variable node", "XTC Parse");
                            stateObject = null;
                        }
                        else
                        {
                            stateName = stateVarNameAttribNode.Value;

                            //Get the Value attribute
                            stateVarValueAttribNode = stateVarAttribs.GetNamedItem("Value");
                            if (stateVarValueAttribNode == null)
                            {
                                LogComment("No Value attribute found for the given state variable node", "XTC Parse");
                                stateObject = null;
                            }
                            else
                            {
                                stateValue = stateVarValueAttribNode.Value;
                                //Construct the object from the name/value pair
                                stateObject[stateName] = stateValue;
                            }
                        }
                    }
                } //end for state variables

                if (stateVars.Count < 1)
                {
                    LogComment("No State Variables found for the given state node", "XTC Parse");
                    stateObject = null;
                }
            }

            return stateObject;
        }

        /// <summary>
        /// Returns an Action Params object from the specified Param xml string
        /// </summary>
        /// <param name="xmlParamString">the xml string for the Params</param>
        /// <returns>State object (containing params), null if failure</returns>
        private State GetActionParams(string xmlParamString)
        {
            if (xmlParamString == null || xmlParamString == "")
            {
                LogComment("No Param xml string given", "Missing Param");
                return null;
            }

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlParamString);

            XmlNodeList paramsList = xmlDoc.GetElementsByTagName("PARAM");
            if (paramsList == null || paramsList.Count < 1)
            {
                LogComment("No Param nodes found in the Xml string", "XTC Parse");
                return null;
            }

            return GetActionParams(paramsList);
        }

        /// <summary>
        /// Returns an Action Params object from the specified Param node list
        /// </summary>
        /// <param name="xmlParamList">the XmlNodeList for the Params</param>
        /// <returns>State object (containing params), null if failure</returns>
        private State GetActionParams(XmlNodeList xmlParamList)
        {
            State paramObject = new State();

            if (xmlParamList == null || xmlParamList.Count < 1)
            {
                LogComment("No Param node(s) given", "Missing Param");
                paramObject = null;
            }
            else
            {
                // Define variables foreach use in the foreach loop
                XmlAttributeCollection paramAttribs;
                XmlNode paramNode, paramNameAttribNode, paramValueAttribNode;
                string paramName, paramValue;
                for (int iParam = 0; iParam < xmlParamList.Count; iParam++)
                {
                    // Get the individual param
                    paramNode = xmlParamList.Item(iParam);

                    //Get the attributes for the param
                    paramAttribs = paramNode.Attributes;
                    if (paramAttribs == null || paramAttribs.Count < 1)
                    {
                        LogComment("No Attributes found for the given param node", "XTC Parse");
                        paramObject = null;
                    }
                    else
                    {
                        //Get the Name attribute
                        paramNameAttribNode = paramAttribs.GetNamedItem("Name");
                        if (paramNameAttribNode == null)
                        {
                            LogComment("No Name attribute found for the given param node", "XTC Parse");
                            paramObject = null;
                        }
                        else
                        {
                            paramName = paramNameAttribNode.Value;

                            //Get the Value attribute
                            paramValueAttribNode = paramAttribs.GetNamedItem("Value");
                            if (paramValueAttribNode == null)
                            {
                                LogComment("No Value attribute found for the given param node", "XTC Parse");
                                paramObject = null;
                            }
                            else
                            {
                                paramValue = paramValueAttribNode.Value;

                                //Construct the object from the name/value pair
                                paramObject[paramName] = paramValue;
                            }
                        }
                    }
                }
            }

            return paramObject;
        }

        /// <summary>
        /// Returns the Action name from the Action node
        /// </summary>
        /// <param name="xmlActionNode">The XmlNode for the ACTION</param>
        /// <param name="fileID">Out. Contains the fileID from the node if found (-1 if not)</param>
        /// <returns>Action name string, null if failure or start state</returns>
        private string GetAction(XmlNode xmlActionNode, out int fileID)
        {
            string actionName = null;
            fileID = -1;

            //Check required param
            if (xmlActionNode == null)
            {
                LogComment("No Action node given", "Missing Param");
                actionName = null;
            }
            else
            {
                //Get the attributes for the action node
                XmlAttributeCollection actionAttribs = xmlActionNode.Attributes;
                if (actionAttribs == null || actionAttribs.Count < 1)
                {
                    //LogComment("Start state found", "GetAction");
                    actionName = null;
                }
                else
                {
                    //Find the attribute Name
                    XmlNode actionAttribNode = actionAttribs.GetNamedItem("Name");
                    if (actionAttribNode == null)
                    {
                        LogComment("No Name attribute found for the given action node", "XTC Parse");
                        actionName = null;
                    }
                    else
                    {
                        actionName = actionAttribNode.Value;
                    }

                    //Check for the FileID attribute
                    XmlNode fileAttribNode = actionAttribs.GetNamedItem("FileID");
                    if (fileAttribNode == null)
                        fileID = -1;
                    else
                        fileID = Int32.Parse(fileAttribNode.Value);
                }
            }

            return actionName;
        }

        /// <summary>
        /// Private LogComment method provides Logging of Comments to the Logger Object
        /// </summary>
        /// <remarks>Raises OnLogComment event</remarks>
        /// <param name="comment">Text to be logged</param>
        /// <param name="type">Type of comment</param>
        /// <example>Logging a comment with a type specified
        /// <code>LogComment("FormatText(x,y,z)", "EnterFunction");</code>
        /// </example>
        private void LogComment(string comment, string type)
        {
            LogComment(comment);
        }

        private void LogComment(string comment)
        {
            //GlobalLog.LogStatus(comment);
            Console.WriteLine(comment);
        }


        private void LogStatus(string comment)
        {
            //GlobalLog.LogDebug(comment);
            Console.WriteLine(comment);
        }


        XmlDocument _xmlDoc = null;

        int _firstCase = 1;

        int _lastCase = -1;

        XmlNodeList _tests = null;

        private XmlNamespaceManager _nsmgr = null;

        private string _prefix = "";

        string _xtcFilename = String.Empty;

        ActionsQueue _queue;

        Hashtable _models = null;


    }
}
