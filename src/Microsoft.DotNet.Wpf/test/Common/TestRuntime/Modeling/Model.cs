// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml;
using System.Collections;
using System.Security;
using System.IO;

namespace Microsoft.Test.Modeling
{
    /// <summary>
    /// State Event handler delegate
    /// </summary>
    public delegate void StateEventHandler(object sender, StateEventArgs e);

    /// <summary>
    /// Validate Event handler delegate
    /// </summary>
    public delegate bool ValidateEventHandler(object sender, StateEventArgs e);

    /// <summary>
    /// ExecuteAction Event handler delegate
    /// </summary>
    public delegate void ExecuteActionEventHandler(object sender, ExecuteActionEventArgs e);


    /// <summary>
    /// Event Arguments for StateEventHandlers
    /// </summary>
    public class StateEventArgs : EventArgs
    {
        private State state;
        private string action = "";

        /// <summary>
        /// Creates a new StateEventArgs
        /// </summary>
        /// <param name="state">The state to pass to the event</param>
        public StateEventArgs(State state)
        {
            this.state = state;
        }

        /// <summary>
        /// Creates a new StateEventArgs
        /// </summary>
        /// <param name="state">The state to pass to the event</param>
        /// <param name="action">The action that is causing the event</param>
        public StateEventArgs(State state, string action)
        {
            this.state = state;
            this.action = action;
        }

        /// <summary>
        /// The State that was passed to the event
        /// </summary>
        public State State
        {
            get { return state; }
        }

        /// <summary>
        /// The Action that caused the event
        /// </summary>
        public string Action
        {
            get { return action; }
        }
    }

    /// <summary>
    /// Event Arguments for ExecuteActionEventHandlers
    /// </summary>
    public class ExecuteActionEventArgs : EventArgs
    {
        private string action;
        private State state;
        private State inParams;
        private State outParams;

        /// <summary>
        /// Creates a new ExecuteActionEventArgs
        /// </summary>
        /// <param name="action">Name of the action to pass the event</param>
        /// <param name="state">End state to pass the event</param>
        /// <param name="inParams">Input parameters for the action</param>
        /// <param name="outParams">Output parameters for the action</param>
        public ExecuteActionEventArgs(string action, State state, State inParams, State outParams)
        {
            this.action = action;
            this.state = state;
            this.inParams = inParams;
            this.outParams = outParams;
        }

        /// <summary>
        /// The name of the action that was passed to the event
        /// </summary>
        public string Action
        {
            get { return action; }
        }

        /// <summary>
        /// The end State that was passed to the event
        /// </summary>
        public State State
        {
            get { return state; }
        }

        /// <summary>
        /// The input parameters that were passed to the event
        /// </summary>
        public State InParams
        {
            get { return inParams; }
        }

        /// <summary>
        /// The output parameters that were passed to the event
        /// </summary>
        public State OutParams
        {
            get { return outParams; }
        }
    }


    ///<summary>
    /// 
    ///</summary>
    public delegate bool ActionHandler(State state, State inParams, State outParams);

    /// <summary>
    /// Model class for state variables and actions
    /// </summary>
    public class Model : ModelingBaseObject
    {


        #region Properties

        /////<summary>
        ///// Logger, we are using this a a temporal logger
        /////</summary>
        //public TestLog Logger
        //{
        //    get
        //    {
        //        return TestLog.Current;
        //    }
        //}

        ///<summary>
        /// AsyncActionsManager the reason for this to enable Async execution through this API
        ///</summary>
        private AsyncActionsManager _asyncActions;

        ///<summary>
        /// AsyncActionsManager the reason for this to enable Async execution through this API        /// 
        ///</summary>
        public AsyncActionsManager AsyncActions
        {
            get
            {
                return _asyncActions;
            }
        }

        /// <summary>
        /// The name of the .ite file for this Model
        /// </summary>
        public string ModelPath
        {
            get { return modelPath; }
            set { modelPath = value; }
        }
        private string modelPath = "";

        ///<summary>
        /// Handler hashtable
        ///</summary>       
        public Hashtable ActionHandlers
        {
            get
            {
                return _actionHandlers;
            }
        }

        ///<summary>
        /// Handler hashtable
        ///</summary>       
        private Hashtable _actionHandlers = new Hashtable();

        private ArrayList actions = new ArrayList();
        /// <summary>
        /// The names of the Actions that this Model contains
        /// </summary>
        public string[] Actions
        {
            get
            {
                return (string[])actions.ToArray(typeof(string));
            }
        }

        private ArrayList stateVariables = new ArrayList();
        /// <summary>
        /// The names of the State Variables that this Model contains
        /// </summary>
        public string[] StateVariables
        {
            get
            {
                return (string[])stateVariables.ToArray(typeof(string));
            }
        }

        /// <summary>
        /// The current start state for this Model
        /// </summary>
        public State CurrStartState
        {
            get { return currStartState; }
            set { currStartState = value; }
        }
        private State currStartState = null;


        #endregion

        private ArrayList definedStateVars = new ArrayList();


        #region Events
        /// <summary>
        /// Fired whenever Initialize is called
        /// </summary>
        protected event EventHandler OnInitialize;

        /// <summary>
        /// Fired whenever CleanUp is called
        /// </summary>
        protected event EventHandler OnCleanUp;


        /// <summary>
        /// Fired whenever GetCurrentState is called
        /// </summary>
        protected event StateEventHandler OnGetCurrentState;


        /// <summary>
        /// Fired whenever SetCurrentState is called
        /// </summary>
        protected event StateEventHandler OnSetCurrentState;

        /// <summary>
        /// Fired whenever BeginCase is called
        /// </summary>
        protected event StateEventHandler OnBeginCase;

        /// <summary>
        /// Fired whenever EndCase is called
        /// </summary>
        protected event StateEventHandler OnEndCase;

        /// <summary>
        /// Fired whenever ExecuteAction is called
        /// </summary>
        protected event ExecuteActionEventHandler OnExecuteAction;

        /// <summary>
        /// Fired whenever a ITEAsyncActions is set on the Model
        /// </summary>
        protected event EventHandler OnSetAsyncHelper;

        /// <summary>
        /// Fired when all the transitions are complete but we still are dispatching inside a nested pump
        /// </summary>
        protected event EventHandler OnEndCaseOnNestedPump;


        #endregion


        /// <summary>
        /// Creates a Model instance
        /// </summary>
        public Model()
        {

        }


        /// <summary>
        /// Initializes the Model
        /// </summary>
        /// <remarks>Raises OnInitialize event</remarks>
        /// <returns>false if errors occurred</returns>
        public virtual bool Initialize()
        {
            if (OnInitialize != null)
                OnInitialize(this, new EventArgs());
            return true;
        }


        /// <summary>
        /// Cleans up the Model
        /// </summary>
        /// <remarks>Raises OnCleanUp event</remarks>
        /// <returns>false if errors occurred</returns>
        public virtual bool CleanUp()
        {
            if (OnCleanUp != null)
                OnCleanUp(this, new EventArgs());
            return true;
        }


        /// <summary>
        /// Adds an action Handler to an action of the Model
        /// </summary>
        /// <param name="name">Name of action to add</param>
        /// <param name="handler">Reference to the handler Function</param>
        /// <example>
        /// <code>myModel.AddAction("Open", new ActionHandler(Open));</code>
        /// </example>
        public virtual void AddAction(string name, ActionHandler handler)
        {
            if (_actionHandlers.Contains(name))
                _actionHandlers[name] = Delegate.Combine((Delegate)_actionHandlers[name], handler);
            else
            {
                _actionHandlers.Add(name, handler);
                actions.Add(name);
            }
        }


        /// <summary>
        /// Executes an action and validates the State of the model
        /// </summary>
        /// <remarks>Raises OnExecuteAction event</remarks>
        /// <param name="action">Name of action to execute</param>
        /// <returns>false if errors occurred</returns>
        /// <example>
        /// <code>myModel.ExecuteAction("Open");</code>
        /// </example>
        public virtual bool ExecuteAction(string action)
        {
            return ExecuteAction(action, new State(), new State(), new State());
        }


        /// <summary>
        /// Executes an action and validates the State of the model
        /// </summary>
        /// <remarks>Raises OnExecuteAction event</remarks>
        /// <param name="action">Name of action to execute</param>
        /// <param name="state">Expected end State object</param>
        /// <returns>false if errors occurred</returns>
        /// <example>
        /// <code>myModel.ExecuteAction("Open", endState);</code>
        /// </example>
        public virtual bool ExecuteAction(string action, State state)
        {
            return ExecuteAction(action, state, new State(), new State());
        }

        /// <summary>
        /// Executes an action and validates the State of the model
        /// </summary>
        /// <param name="action">Name of action to execute</param>
        /// <param name="state">Expected end State object</param>
        /// <param name="inParams">Input Action Parameter object</param>
        /// <returns>false if errors occurred</returns>
        /// <example>
        /// <code>myModel.ExecuteAction("Open", endState, inAParams);</code>
        /// </example>
        public virtual bool ExecuteAction(string action, State state, State inParams)
        {
            return ExecuteAction(action, state, inParams, new State());
        }

        /// <summary>
        /// Executes an action and validates the State of the model
        /// </summary>
        /// <remarks>Raises OnExecuteAction event</remarks>
        /// <param name="action">Name of action to execute</param>
        /// <param name="state">Expected end State object</param>
        /// <param name="inParams">Input Action Parameter object</param>
        /// <param name="outParams">Output Action Parameter object</param>
        /// <returns>false if errors occurred</returns>
        /// <example>
        /// <code>myModel.ExecuteAction("Open", endState, inAParams, outAParams);</code>
        /// </example>
        public virtual bool ExecuteAction(string action, State state, State inParams, State outParams)
        {
            bool actionResult = true;

            //check whether the action exists
            if (!_actionHandlers.Contains(action))
                throw new Exception("This model does not contain an action called " + action);

            //Save the outParams in case they are needed for comparison later
            State expOutputParams = outParams;

            //Invoke the Action Handlers
            ActionHandler aHandler = (ActionHandler)_actionHandlers[action];
            foreach (ActionHandler handler in aHandler.GetInvocationList())
            {
                actionResult = handler(state, inParams, outParams);
            }

            //not sure what purpose this serves (should remove if not nessasary)
            if (OnExecuteAction != null)
                OnExecuteAction(this, new ExecuteActionEventArgs(action, state, inParams, outParams));

            //Validate the expected state against the current state if the action succeds
            if (actionResult && state != null)
                return ValidateState(state);

            return actionResult;
        }


        //Compare the expected state agaist the actual state
        //Comparision only can produce a failure if the both expected
        //and current states have the same state value and they are different
        private bool ValidateState(State expectedState)
        {
            State curState = CurrentState;
            foreach (DictionaryEntry sVar in expectedState)
            {
                string strState = (string)sVar.Key;
                if (expectedState[strState] != null && curState[strState] != null)
                {
                    if (expectedState[strState].ToString() != curState[strState].ToString())
                    {
                        LogComment("State Validation Failed\n" + strState + "\tExpected=" + expectedState[strState] + "\tActual=" + curState[strState]);
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Adds a State Variable
        /// </summary>
        /// <param name="name">Name of the state variable to add</param>
        /// <example>
        /// <code>myModel.AddStateVariable("Browser_State");</code>
        /// </example>
        public virtual void AddStateVariable(string name)
        {
            stateVariables.Add(name);
        }


        /// <summary>
        /// Returns the current state of the model
        /// </summary>
        /// <remarks>Raises OnGetCurrentState event</remarks>
        /// <returns>State object that has been filled in with the current state</returns>
        /// <example>
        /// <code>
        /// State currState = myModel.GetCurrentState();
        /// </code>
        /// </example>
        public State CurrentState
        {
            get
            {
                State _currentState = new State();
                if (OnGetCurrentState != null)
                    OnGetCurrentState(this, new StateEventArgs(_currentState));

                return _currentState;
            }

            set
            {
                if (OnSetCurrentState != null)
                    OnSetCurrentState(this, new StateEventArgs(value));
            }
        }


        ///<summary>
        /// This Set the AsyncHelper to people can get the Async API on the Model
        ///</summary>
        public virtual void SetAsyncHelper(AsyncActionsManager actions)
        {
            _asyncActions = actions;

            if (OnSetAsyncHelper != null)
                OnSetAsyncHelper(actions, EventArgs.Empty);
        }


        /// <summary>
        /// Notifys the model that a case is beginning
        /// </summary>
        /// <remarks>Raises OnBeginCase event</remarks>
        /// <param name="state">A state object containing the start state</param>
        /// <returns>false if errors occurred</returns>
        /// <example>
        /// <code>myModel.BeginCase(startState);</code>
        /// </example>
        public virtual bool BeginCase(State state)
        {
            if (OnBeginCase != null)
                OnBeginCase(this, new StateEventArgs(state));


            //Validate the expected state against the current state
            if (state != null)
                return ValidateState(state);
            else
                return true;
        }


        /// <summary>
        /// Notifys the model that a case has ended
        /// </summary>
        /// <remarks>Raises OnEndCase event</remarks>
        /// <param name="state">A state object containing the end state</param>
        /// <returns>false if errors occurred</returns>
        /// <example>
        /// <code>myModel.EndCase(endState);</code>
        /// </example>
        public virtual bool EndCase(State state)
        {

            if (OnEndCase != null)
                OnEndCase(this, new StateEventArgs(state));

            return true;
        }


        ///<summary>
        /// 
        ///</summary>
        public virtual void EndCaseOnNestedPump()
        {
            if (OnEndCaseOnNestedPump != null)
            {
                OnEndCaseOnNestedPump(this, EventArgs.Empty);
            }
        }



        /// <summary>
        /// Private LogComment method provides Logging of Comments to the Logger Object
        /// </summary>
        /// <remarks>Raises OnLogComment event</remarks>
        /// <param name="comment">Text to be logged</param>
        /// <example>Logging a comment without a type or caller specified
        /// <code>LogComment("About to enter the FormatText function");</code>
        /// </example>
        protected void LogComment(string comment)
        {
            LogComment(comment, null);
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
        protected void LogComment(string comment, string type)
        {
            Console.WriteLine(comment);
            //Logger.LogComment(comment);
        }



    }


    /// <summary>
    /// State Object
    /// </summary>
    /// <remarks>This class is also used for Action Parameters</remarks>
    public class State : IDictionary, IEnumerable, ICollection
    {
        private ArrayList stateVarValues = new ArrayList();
        private ArrayList stateVarNames = new ArrayList();

        /// <summary>
        /// Creates a new State Object
        /// </summary>
        public State()
        {
        }

        /// <summary>
        /// Gets a value indicating whether the State object has a fixed size
        /// </summary>
        public bool IsFixedSize
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether the State object is read-only
        /// </summary>
        public bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Retrieves the value of a State Variable from the collection
        /// </summary>
        /// <param name="name">Name of the State Variable</param>
        /// <returns>Value of the specified State Variable</returns>
        public object this[object name]
        {
            get
            {
                int index = stateVarNames.IndexOf(name);
                if (index >= 0 && stateVarValues[index] != null)
                    return stateVarValues[index];
                return null;
            }

            set
            {
                string input = null;
                if (null != value)
                    input = value.ToString();
                int index = stateVarNames.IndexOf(name);
                if (index >= 0)
                    stateVarValues[index] = input;
                else
                {
                    stateVarNames.Add(name);
                    stateVarValues.Add(input);
                }
            }
        }

        /// <summary>
        /// Retrieves the value of a State Variable from the collection
        /// </summary>
        /// <param name="name">Name of the State Variable to retrieve</param>
        /// <returns>Value of the specified State Variable</returns>
        public string this[string name]
        {
            get
            {
                int index = stateVarNames.IndexOf(name);
                if (index >= 0 && stateVarValues[index] != null)
                    return (string)stateVarValues[index];
                return null;
            }

            set
            {
                int index = stateVarNames.IndexOf(name);
                if (index >= 0)
                    stateVarValues[index] = value;
                else
                {
                    stateVarNames.Add(name);
                    stateVarValues.Add(value);
                }
            }
        }


        /// <summary>
        /// Adds an variable with the provided name and value to the State object
        /// </summary>
        /// <param name="stateVarName">The name of the state variable to add</param>
        /// <param name="stateVarValue">The value of the state variable</param>
        public void Add(object stateVarName, object stateVarValue)
        {
            if (!stateVarNames.Contains(stateVarName))
            {
                stateVarNames.Add(stateVarName);
                stateVarValues.Add(stateVarValue);
            }
        }

        /// <summary>
        /// Removes all variables from the State object
        /// </summary>
        public void Clear()
        {
            stateVarNames.Clear();
            stateVarValues.Clear();
        }

        /// <summary>
        /// Determines whether the State contains a variable with the specified name
        /// </summary>
        /// <param name="name">The name of the variable to look for</param>
        /// <returns>true if the State contains a variable with the name; otherwise, false</returns>
        public bool Contains(object name)
        {
            return stateVarNames.Contains(name);
        }

        /// <summary>
        /// Removes the variable with the specified name from the State
        /// </summary>
        /// <param name="name">The name of the variable to remove</param>
        public void Remove(object name)
        {
            int index = stateVarNames.IndexOf(name);
            if (index >= 0)
            {
                stateVarNames.Remove(name);
                stateVarValues.RemoveAt(index);
            }
        }

        /// <summary>
        /// ICollection containing the keys making up the State
        /// </summary>
        public ICollection Keys
        {
            get { return stateVarNames; }
        }

        /// <summary>
        /// ICollection containing the values making up the State
        /// </summary>
        public ICollection Values
        {
            get { return stateVarValues; }
        }

        /// <summary>
        /// Returns an enumerator that can iterate through a collection
        /// </summary>
        /// <returns>An IDictionaryEnumerator that can be used to iterate through the collection.</returns>
        public IDictionaryEnumerator GetEnumerator()
        {
            return (new StateEnumerator(this));
        }

        /// <summary>
        /// Implementation of GetEnumerator for IEnumerator interface
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Copies the State variables to a one-dimensional System.Array instance at the specified index
        /// </summary>
        /// <param name="array">The one-dimensional System.Array that is the destination of the DictionaryEntry objects copied from State</param>
        /// <param name="index">The zero-based index in array at which copying begins</param>
        void ICollection.CopyTo(Array array, int index)
        {
            int pos = index;
            foreach (DictionaryEntry item in this)
            {
                array.SetValue(item, pos++);
            }
        }

        /// <summary>
        /// Copies the State variables to a one-dimensional DictionaryEntry array at the specified index
        /// </summary>
        /// <param name="array">The DictionaryEntry array that is the destination of the DictionaryEntry objects copied from State</param>
        /// <param name="index">The zero-based index in array at which copying begins</param>
        public void CopyTo(DictionaryEntry[] array, int index)
        {
            int pos = index;
            foreach (DictionaryEntry item in this)
            {
                array.SetValue(item, pos++);
            }
        }

        /// <summary>
        /// Number of State variables in the collection
        /// </summary>
        public int Count
        {
            get { return stateVarValues.Count; }
        }

        /// <summary>
        /// Gets Object that can be used to synchronize access
        /// </summary>
        public object SyncRoot
        {
            get { return stateVarValues.SyncRoot; }
        }

        /// <summary>
        /// Gets a value indicating whether access to the Array is synchronized (thread-safe).
        /// </summary>
        public bool IsSynchronized
        {
            get { return stateVarValues.IsSynchronized; }
        }

        /// <summary>
        /// Ouputs the State object as a space deliminated string of name:value pairs
        /// </summary>
        /// <returns>The State as a string</returns>
        public override string ToString()
        {
            string stateString = "";
            string stateName;
            for (int i = 0; i < stateVarNames.Count; i++)
            {
                stateName = (string)stateVarNames[i];
                if (stateVarValues[i] != null)
                    stateString += stateName + ":" + stateVarValues[i];
                else
                    stateString += stateName + ":";
                stateString += " ";
            }

            return stateString;
        }

        /// <summary>
        /// Ouputs the State object as a space deliminated string in the form of the specified Model
        /// </summary>
        /// <param name="model">The Model to get the state variable order from</param>
        /// <returns>The State as a string</returns>
        public string ToString(Model model)
        {
            string stateString = "";
            int index;
            foreach (string stateName in model.StateVariables)
            {
                index = stateVarNames.IndexOf(stateName);
                if (index >= 0 && stateVarValues[index] != null)
                    stateString += stateVarValues[index];
                stateString += " ";
            }

            return stateString;
        }

        class StateEnumerator : IDictionaryEnumerator
        {
            private State state;
            private int pos = -1;

            public StateEnumerator(State state)
            {
                this.state = state;
            }

            public bool MoveNext()
            {
                if (++pos >= state.stateVarValues.Count)
                    return false;
                else
                    return true;
            }

            public void Reset()
            {
                pos = -1;
            }

            public object Current
            {
                get { return this.Entry; }
            }

            public DictionaryEntry Entry
            {
                get { return (new DictionaryEntry(this.Key, this.Value)); }
            }

            public object Key
            {
                get { return state.stateVarNames[pos]; }
            }

            public object Value
            {
                get { return state.stateVarValues[pos]; }
            }

        }
    }

    /// <summary>
    /// This class just serves as an alias for State functionality, so that new tester code/framework doesn't
    /// use State for Parameters
    /// </summary>
    public class Parameters : State { }
}
