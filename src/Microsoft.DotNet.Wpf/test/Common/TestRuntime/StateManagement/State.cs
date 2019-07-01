// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.Reflection;
using System.ComponentModel;

//TODO: Need to implement full state management. The current implementation assumes you can define
//an initial state, and transition to that initial state. It also forces you to use the state class
//to define your state space. We would ideally need a way for a state class to enumerate all possible states
//in a given space, and return the state values for each of those states, so that you are not limited
//to explicitly defining a given state space.

namespace Microsoft.Test.StateManagement
{
    /// <summary>
    /// This class allows you to define actions to perform when transitionning
    /// from one state value to another.
    /// </summary>
    /// <typeparam name="V">The type for the value returned by the state</typeparam>
    /// <typeparam name="A">The type for the action used to perform the transition</typeparam>
    public abstract class State<V,A> : IState
    {
        #region Constructor

        /// <summary>
        /// Parameterless constructor needed for serialization
        /// </summary>
        public State()
        {
        }

        #endregion

        #region Public Members

        /// <summary>
        /// Returns the current value of the state
        /// </summary>
        /// <returns></returns>
        public virtual V GetValue()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Transitions from current value to new value
        /// </summary>
        /// <param name="value"></param>
        /// <param name="action"></param>
        public virtual bool SetValue(V value, A action)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Determines if this state supports transitions
        /// </summary>
        public virtual bool CanTransition
        {
            get { return true; }
        }

        #endregion

        #region IEqualityComparer<State<V>> Members

        /// <summary/>
        public override abstract bool Equals(object obj);

        /// <summary/>
        public override int GetHashCode()
        {
            //We match entry if our type and our value type are the same.
            return (GetType().Name.GetHashCode() ^ typeof(V).Name.GetHashCode());
        }

        #endregion

        #region Operator Equality overloading

        /// <summary/>
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static bool operator ==(State<V, A> x, State<V, A> y)
        {
            if (object.Equals(x, null))
                return object.Equals(y, null);
            else
                return x.Equals(y);
        }

        /// <summary/>
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static bool operator !=(State<V, A> x, State<V, A> y)
        {
            if (object.Equals(x, null))
                return !object.Equals(y, null);
            else
                return !x.Equals(y);
        }

        #endregion

        #region IState Members

        object IState.GetValue()
        {
            return GetValue();
        }

        bool IState.SetValue(object newValue, object action)
        {
            return SetValue((V)newValue, (A) action);
        }

        [XmlIgnore()]
        Type IState.ValueType
        {
            get { return typeof(V); }
        }

        #endregion
    }

    /// <summary>
    /// Interface to Get/Set generic State objects
    /// </summary>
    public interface IState
    {
        /// <summary/>
        object GetValue();
        /// <summary/>
        bool SetValue(object newValue, object action);
        /// <summary>
        /// Returns the type of the state's value
        /// </summary>
        Type ValueType { get; }
        /// <summary>
        /// Determines if this state can be changed
        /// </summary>
        bool CanTransition { get; }
    }
}
