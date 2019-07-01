// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Test.Stability.Core
{
    /// <summary>
    /// Provides a stateful sandbox environment for executing sequences of actions
    /// </summary>
    [Serializable]
    public class ExecutionContext
    {
        #region Private Data

        private IState state;
        private IActionSequencer actionSequencer;

        private Sequence currentSequence;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes the mediated state based on input metadata for the state object and sequencer object
        /// </summary>
        /// <param name="metadata"></param>
        public ExecutionContext(ExecutionContextMetadata metadata)
        {
            if (metadata == null)
            {
                throw new ArgumentNullException("metadata");
            }

            //create the state object, set initialization arguments and reset
            state = (IState)Activator.CreateInstance(metadata.StateType);
            state.Initialize(metadata.StateArguments);

            //Create the Action Sequencer
            actionSequencer = (IActionSequencer)Activator.CreateInstance(metadata.ActionSequencerType);
        }

        #endregion

        #region Public Members

        /// <summary>
        /// Attempts to generate a new sequence from ActionSequencer when no sequence is on hand.
        /// Will throw in presence of existing Sequence.
        /// </summary>
        /// <param name="random"></param>
        /// <returns>Returns true if a new sequence could be prepared. Returns false in event of failure to create sequence.</returns>
        public void GetSequence(DeterministicRandom random)
        {
            if (currentSequence != null)
            {
                throw new InvalidOperationException("Attempted to create a new sequence in presence of existing sequence.");
            }

            currentSequence = actionSequencer.GetNext(state, random);
            if (currentSequence == null)
            {
                throw new InvalidOperationException("Attempted to create a new sequence in presence of existing sequence.");
            }
        }

        /// <summary>
        /// Performs next action in Sequence. Discards reference to sequence when end is reached.
        /// </summary>
        /// <param name="random"></param>
        /// <returns>Returns true if sequence has more actions to perform</returns>
        public bool DoNext(DeterministicRandom random)
        {
            bool hasMoreActions = false;
            //if current sequence is not null, do next action. 
            if (currentSequence == null)
            {
                throw new InvalidOperationException("Attempting to execute on a null sequence.");
            }
            else
            {
                hasMoreActions = currentSequence.DoNext(random);

                //discard sequence reference at end of sequence
                if (hasMoreActions == false)
                {
                    currentSequence = null;
                }

            }
            return hasMoreActions;
        }
        
        /// <summary>
        /// Brings the State back to default state. Used for reining in resource consumption.
        /// </summary>
        /// <param name="stateIndicator">An integer determinates how to reset.</param>
        public void ResetState(int stateIndicator)
        {
            state.Reset(stateIndicator);
        }

        /// <summary>
        /// Indicates if State exceeds own threshold for excess resource use. This triggers a reset.
        /// </summary>
        /// <returns></returns>
        public bool IsStateBeyondConstraints()
        {
            return state.IsBeyondConstraints();
        }

        #endregion

    }
}
