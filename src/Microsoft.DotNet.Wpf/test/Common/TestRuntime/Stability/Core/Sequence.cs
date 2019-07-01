// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Test.Stability.Core
{
    /// <summary>
    /// Describes a Sequence of Actions
    /// </summary>
    public class Sequence
    {
        #region Private variables

        private IAction rollbackAction = null;
        private Queue<IAction> queue = new Queue<IAction>();

        #endregion

        #region Constructors

        /// <summary>
        /// Default Ctor.
        /// </summary>
        public Sequence() { }

        /// <summary>
        /// Create a sequence from a list of actions.
        /// </summary>
        /// <param name="actions"></param>
        public Sequence(params IAction[] actions)
        {
            foreach (IAction action in actions)
            {
                AddAction(action);
            }
        }

        #endregion

        #region Public Members

        /// <summary>
        /// Called by mediator to perform next action in sequence
        /// </summary>
        /// <param name="random"></param>
        /// <returns>
        /// True when there are additional sequence actions to perform
        /// False when performing the last action in sequence
        /// False in the event of an action state check failure(rollback will be performed automatically)
        /// </returns>
        public bool DoNext(DeterministicRandom random)
        {
            if (queue.Count == 0)
            {
                throw new InvalidOperationException("This sequence does not contain a next action");
            }

            IAction next = queue.Dequeue();
            //If Action is in a suitable state, perform action.
            //Otherwise, engage rollback.
            if (next.CanPerform())
            {
                Trace.WriteLine("[Sequence] Performing Action:"+next.GetType());
                next.Perform(random);
            }
            else if (rollbackAction != null && rollbackAction.CanPerform())
            {
                Trace.WriteLine("[Sequence] Rolling Back Sequence");
                rollbackAction.Perform(random);
                queue.Clear();
            }

            return queue.Count > 0;
        }

        /// <summary>
        /// Called by ActionSequencers to add Actions to the end of sequence
        /// </summary>
        /// <param name="action"></param>
        public void AddAction(IAction action)
        {
            queue.Enqueue(action);
        }

        /// <summary>
        /// Defines rollback action in event of failure to perform sequence
        /// </summary>
        public IAction RollbackAction
        {
            get { return rollbackAction; }
            set { rollbackAction = value; }
        }

        #endregion
    }
}
