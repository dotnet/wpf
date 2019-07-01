// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using Microsoft.Test.Stability.Core;
using System.Collections;
using System.Xml;

namespace Microsoft.Test.Stability.Extensions.State
{
    /// <summary>
    /// This is a scaffolding base state class for implementing the Annotations Stress.
    /// TODO: Normalize this implementation for broader use or factor off to Annotations.
    /// General version will support Factories/Surface Area/General State pool, in a Attribute based fashion
    /// </summary>
    public class BaseState : IState
    {
        #region Private Variables
        TypedListDictionary statePool = new TypedListDictionary();
        #endregion

        #region Public Members

        /// <summary>
        /// Adds an object to the state resource pool
        /// </summary>
        /// <param name="o"></param>
        public void Add(Object o)
        {
            Type t = o.GetType();
            statePool.Add(t, o);
            t = t.BaseType;
            while (t != null)
            {
                statePool.Add(t, o);
                t = t.BaseType;
            }
        }

        /// <summary>
        /// Removes an object from the state resource pool.
        /// </summary>
        /// <param name="o"></param>
        public void Remove(Object o)
        {
            Type t = o.GetType();
            while (t != null)
            {
                statePool.Remove(t, o);
                t = t.BaseType;
            }
        }

        /// <summary>
        /// Gets a random object of requested type from the state resource pool, if it exists.
        /// </summary>
        /// <param name="t">Type of requested object</param>
        /// <param name="random">DeterministicRandom object</param>
        /// <returns></returns>
        public Object GetRandom(Type t, DeterministicRandom random)
        {
            ArrayList l = statePool.GetObjectsOfType(t);
            if (l == null || l.Count == 0)
            {
                //TODO: Do we really want tests to themselves to get into such a state?
                return null;
            }

            //Return a randomly selected object of types' available choices.
            return l[random.Next(l.Count)];
        }

        #endregion

        #region IState Members

        //Fortunately We don't do anything with this...
        public void Initialize(IStatePayload arguments)
        {
        }

        /// <summary/>
        public virtual void Reset(int stateIndicator)
        {
            statePool.Clear();
        }

        /// <summary/>
        public virtual bool IsBeyondConstraints()
        {
            //TODO: Constraint Checking.
            return false;
        }

        #endregion
    }
}
