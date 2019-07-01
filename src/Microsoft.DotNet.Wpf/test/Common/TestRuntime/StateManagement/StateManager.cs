// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using Microsoft.Test.Serialization;

//TODO: Need to implement full state management. See State class for details on what is needed.

namespace Microsoft.Test.StateManagement
{
    /// <summary>
    /// State Manager allows for transitions and tracking of state objects and associated values.
    /// It tracks the state space through the states object, and the difference between initial
    /// and current through a dictionary.
    /// </summary>
    public class StateManager : IDisposable
    {
        #region Private Members

        //Holds a dictionary of state values
        private Dictionary<IState, object> values = new Dictionary<IState, object>();
        private Dictionary<string, List<IState>> stateSpaces = new Dictionary<string, List<IState>>();
        private object lockObject = new object();

        private bool disposed = false;
        private static readonly StateManager current = new StateManager();

        #endregion

        #region Constructor

        /// <summary/>
        private StateManager()
        {
        }

        #endregion

        #region Public Members

        /// <summary>
        /// State Manager Instance
        /// </summary>
        public static StateManager Current
        {
            get { return current; }
        }

        /// <summary>
        /// Adds a state to the current state space
        /// </summary>
        /// <param name="stateSpace"></param>
        /// <param name="iState"></param>
        public void AddState(string stateSpace, IState iState)
        {
            AddStateInternal(stateSpace, iState, iState.GetValue());
        }

        /// <summary>
        /// Returns a dictionary of state values for all the complete state space
        /// </summary>
        /// <returns></returns>
        public Dictionary<IState, object> GetCurrentStateValues()
        {
            if (disposed)
                return null;

            Dictionary<IState, object> currentStates = new Dictionary<IState, object>();

            foreach (List<IState> states in stateSpaces.Values)
                foreach (IState state in states)
                {
                    if (!currentStates.ContainsKey(state))
                        currentStates.Add(state, state.GetValue());
                }

            return currentStates;
        }

        /// <summary>
        /// Returns a dictionary of state values for all the states in the specified space
        /// </summary>
        /// <returns></returns>
        public Dictionary<IState, object> GetCurrentStateValues(string stateSpace)
        {
            if (disposed)
                return null;
            
            if (!stateSpaces.ContainsKey(stateSpace))
                return null;

            Dictionary<IState, object> currentStates = new Dictionary<IState, object>();

            foreach (IState state in stateSpaces[stateSpace])
                currentStates.Add(state, state.GetValue());

            return currentStates;
        }

        /// <summary>
        /// Deserializes the xml and performs the transition of states
        /// </summary>
        /// <param name="stateSpace"></param>
        /// <param name="stateSpaceFilename">State space to deserialize</param>
        /// <param name="revertExistingStates">Revert the states that are currently being tracked but not specified in the state space xml</param>
        public bool Transition(string stateSpace, string stateSpaceFilename, bool revertExistingStates)
        {
            using (XmlTextReader reader = new XmlTextReader(stateSpaceFilename))
            {
                return Transition(stateSpace, Deserialize(reader), revertExistingStates);
            }
        }

        /// <summary>
        /// Transitions from one state value to another for a given state
        /// </summary>
        public bool Transition(string stateSpace, IState state, object stateValue)
        {
            return Transition(stateSpace, state, stateValue, null);
        }

        /// <summary>
        /// Transitions from one state value to another for a given state
        /// </summary>
        public bool Transition(string stateSpace, IState state, object stateValue, object action)
        {
            lock (lockObject)
            {
                return TransitionInternal(stateSpace, state, stateValue, action);
            }
        }

        /// <summary>
        /// Performs transition of states
        /// </summary>
        /// <param name="stateSpace"></param>
        /// <param name="stateTransitions">State and matching state value to transition</param>
        /// <param name="revertOtherStates">Set to true to revert states that are currently part of the state space but not in the provided dictionary</param>
        public bool Transition(string stateSpace, Dictionary<IState, object> stateTransitions, bool revertOtherStates)
        {
            bool transitionned = false;

            if (disposed)
                throw new ObjectDisposedException("StateManager");

            if (stateTransitions == null)
                throw new ArgumentNullException("stateTransitions");

            if (String.IsNullOrEmpty(stateSpace))
                throw new ArgumentNullException("stateSpace");

            if (stateTransitions.Count == 0)
                return false;

            if (!stateSpaces.ContainsKey(stateSpace))
                stateSpaces[stateSpace] = new List<IState>();

            lock (lockObject)
            {
                //When Reverting, need to calculate delta between new dictionary and delta dictionary
                //  Anything that matches -> Transition
                //  Anything that is not in new dictionary -> Revert delta
                //  Anything that is not in new dictionary -> Remove from state space
                //  Anything that is in new dictionary -> Transition
                //  Anything that is in new dictionary -> Add to state space
                //When not reverting, perform a straight transition

                //List is snapshot of original values before starting the transitions.
                List<IState> statesToRevert = (revertOtherStates) ? new List<IState>(stateSpaces[stateSpace]) : new List<IState>();

                foreach (KeyValuePair<IState, object> stateTransition in stateTransitions)
                {
                    if (revertOtherStates && stateSpaces[stateSpace].Contains(stateTransition.Key))
                    {
                        statesToRevert.Remove(stateTransition.Key); //Part of dictionary, so remove
                        transitionned = TransitionInternal(stateSpace, stateTransition.Key, stateTransition.Value, null);
                    }
                    else
                        transitionned = TransitionInternal(stateSpace, stateTransition.Key, stateTransition.Value, null);
                }

                //Remove states that were not in given dictionary but were present before we started the transitions
                foreach (IState state in statesToRevert)
                {
                    transitionned &= TransitionInternal(stateSpace, state, values[state], null);
                    stateSpaces[stateSpace].Remove(state);
                }
            }

            return transitionned;
        }

        /// <summary>
        /// Reverts all the states in the current state space
        /// </summary>
        public void End()
        {
            if (!disposed)
            {
                foreach (string stateSpace in stateSpaces.Keys)
                    End(stateSpace);
            }
        }

        /// <summary>
        /// Ends the management of states by reverting any changes that were performed
        /// </summary>
        public void End(string stateSpace)
        {
            if (!disposed)
            {
                if (!stateSpaces.ContainsKey(stateSpace))
                    return;

                //Copy the dictionary since we will be enumerating it internally as we remove items
                Dictionary<IState, object> stateSpaceValues = new Dictionary<IState, object>(stateSpaces[stateSpace].Count);
                foreach (IState state in stateSpaces[stateSpace])
                    stateSpaceValues[state] = values[state];

                //Transition back to original values
                Transition(stateSpace, stateSpaceValues, true);

                //Remove the states from the current list
                stateSpaces[stateSpace].Clear();
            }
        }        

        #endregion

        #region Private Members

        private void AddStateInternal(string stateSpace, IState iState, object stateValue)
        {
            if (!stateSpaces.ContainsKey(stateSpace))
            {
                stateSpaces[stateSpace] = new List<IState>();
                stateSpaces[stateSpace].Add(iState);
            }
            else if (!stateSpaces[stateSpace].Contains(iState))
                stateSpaces[stateSpace].Add(iState);

            //General tracking of original states
            if (!values.ContainsKey(iState))
                values[iState] = stateValue;
        }

        private bool TransitionInternal(string stateSpace, IState state, object stateValue, object action)
        {
            if (state == null)
                throw new ArgumentNullException("state");

            if (disposed)
                throw new ObjectDisposedException("StateManager");
            
            object originalValue = state.GetValue();

            //TODO: This check does not take into account the action
            //      It is possible that we need to reevaluate this check if we need to bring back the User Session State
            //If we are already in the requested state then we dont need to do anything
            if (object.Equals(originalValue, stateValue))
            {
                return true;
            }

            AddStateInternal(stateSpace, state, originalValue);

            //HACK: We should probably be returning false here, but this would make the validation
            //logic more complicated, since we would knowingly fail whenever we have a non-transitionning state
            if (!state.CanTransition)
                return true;

            if (!state.SetValue(stateValue, action))        //Perform the transition using the given action
                return false;

            object newValue = state.GetValue();
            //TODO: Should we try again in case we fail to transition

            return Object.Equals(newValue, stateValue);
        }

        #endregion

        #region Static Members

        /// <summary>
        /// Deserializes a state space into a dictionary of state and matching state value
        /// </summary>
        /// <param name="stateSpaceXml"></param>
        /// <returns></returns>
        public static Dictionary<IState, object> Deserialize(string stateSpaceXml)
        {
            if (String.IsNullOrEmpty(stateSpaceXml))
                throw new ArgumentNullException("stateSpaceXml");

            using (XmlTextReader reader = new XmlTextReader(new StringReader(stateSpaceXml)))
            {
                reader.WhitespaceHandling = WhitespaceHandling.None;
                return Deserialize(reader);
            }
        }

        /// <summary>
        /// Deserializes a state space into a dictionary of state and matching state value
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static Dictionary<IState, object> Deserialize(XmlTextReader reader)
        {            
            //TODO: Decide how we want to expose states that are to be tracked but not initialized

            if (reader == null)
                throw new ArgumentNullException("reader");

            Dictionary<IState, object> states = new Dictionary<IState, object>();

            reader.WhitespaceHandling = WhitespaceHandling.None;
            if (!reader.ReadToFollowing("StateSpace"))
                throw new InvalidOperationException();

            while (reader.Read())
            {
                if (reader.Name.Equals("StateSpaceElement", StringComparison.InvariantCultureIgnoreCase))
                    reader.Read();
                else
                    continue;

                IState state = null;
                object stateValue = null;

                while (state == null || stateValue == null)
                {
                    if (reader.Name.Equals("State", StringComparison.InvariantCultureIgnoreCase))
                        state = GetDeserializedObject(reader) as IState;
                    else if (reader.Name.Equals("StateValue", StringComparison.InvariantCultureIgnoreCase))
                        stateValue = GetDeserializedObject(reader);
                    else
                        break; //Unknown element
                }

                if (state == null)
                    continue;

                states[state] = stateValue;
            }

            return states;
        }

        /// <summary>
        /// Serializes a state space for the current state manager
        /// </summary>
        /// <param name="stateManager"></param>
        /// <param name="stateSpace"></param>
        /// <returns></returns>
        public static string Serialize(StateManager stateManager, string stateSpace)
        {
            StringWriter stringWriter = new StringWriter();
            using (XmlTextWriter xmlWriter = new XmlTextWriter(stringWriter))
            {
                Serialize(stateManager, stateSpace, xmlWriter);
            }
            return stringWriter.ToString();
        }

        /// <summary>
        /// Serializes a state space for the current state manager
        /// </summary>
        /// <param name="stateManager"></param>
        /// <param name="stateSpace"></param>
        /// <param name="xmlWriter"></param>
        public static void Serialize(StateManager stateManager, string stateSpace, XmlTextWriter xmlWriter)
        {
            if (stateManager == null)
                throw new ArgumentNullException("stateManager");
            if (String.IsNullOrEmpty(stateSpace))
                throw new ArgumentNullException("stateSpace");
            if (xmlWriter == null)
                throw new ArgumentNullException("writer");
            if (StateManager.Current == null)
                throw new ApplicationException("State Manager is not initialized.");

            List<IState> statesList = stateManager.stateSpaces[stateSpace];
            Dictionary<IState,object> states = new Dictionary<IState,object>(statesList.Count);
            foreach (IState state in statesList)
                states.Add(state, state.GetValue());

            Serialize(states, xmlWriter);
        }

        /// <summary>
        /// Serializes a dictionary of states into an xml writer
        /// </summary>
        /// <param name="states"></param>
        /// <param name="xmlWriter"></param>
        public static void Serialize(Dictionary<IState, object> states, XmlTextWriter xmlWriter)
        {
            if (states == null)
                throw new ArgumentNullException("states");
            if (xmlWriter == null)
                throw new ArgumentNullException("xmlWriter");

            //HACK: Works around exception thrown by XmlSerializer
            XmlAttributeOverrides xmlAttributeOverrides = new XmlAttributeOverrides();
            //Avoids outputting xml namespace schema info
            XmlSerializerNamespaces xmlSerializerNamespaces = new XmlSerializerNamespaces();
            xmlSerializerNamespaces.Add(string.Empty, string.Empty);

            xmlWriter.Formatting = Formatting.Indented;
            xmlWriter.WriteStartElement("StateSpace");

            foreach (IState iState in states.Keys)
            {
                xmlWriter.WriteStartElement("StateSpaceElement");
                Type stateType = iState.GetType();
                object stateValue = states[iState];


                xmlWriter.WriteStartElement("State");
                xmlWriter.WriteAttributeString("Type", stateType.ToString());

                ObjectSerializer.Serialize(xmlWriter, iState);
                xmlWriter.WriteEndElement();

                if (stateValue != null)
                {
                    Type stateValueType = stateValue.GetType();
                    xmlWriter.WriteStartElement("StateValue");
                    xmlWriter.WriteAttributeString("Type", stateValueType.ToString());
                    ObjectSerializer.Serialize(xmlWriter, stateValue);
                    xmlWriter.WriteEndElement();
                }

                xmlWriter.WriteEndElement(); //StateSpaceElement
            }

        }

        private static object GetDeserializedObject(XmlReader reader)
        {
            object deserializedObject = null;
            Type objectType = null;

            if (reader.HasAttributes && !reader.IsEmptyElement)
            {
                objectType = Type.GetType(reader.GetAttribute("Type"));
                //XmlAttributeOverrides xmlAttributeOverrides = new XmlAttributeOverrides();
                //XmlSerializer xmlSerializer = new XmlSerializer(objectType, xmlAttributeOverrides);
                
                StringReader stringReader = new StringReader(reader.ReadInnerXml());
                using (XmlTextReader innerReader = new XmlTextReader(stringReader))
                {
                    deserializedObject = ObjectSerializer.Deserialize(innerReader, objectType, null);
                }
                //xmlSerializer.Deserialize(new StringReader(innerXml));
            }

            return deserializedObject;
        }

        #endregion

        #region IDisposable Members

        void IDisposable.Dispose()
        {
            if (!disposed)
                End();
            disposed = true;
        }

        #endregion
    }
}
