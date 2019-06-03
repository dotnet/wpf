// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***************************************************************************\
*
*
* A collection of TriggerAction objects to be associated with a Trigger
*  object
*
*
\***************************************************************************/
using System.Collections;           // IList
using System.Collections.Generic;   // IList<T>
using MS.Internal;
using System.Diagnostics;

namespace System.Windows
{
    /// <summary>
    ///   A set of TriggerAction for use in a Trigger object
    /// </summary>
    public sealed class TriggerActionCollection : IList, IList<TriggerAction>
    {
        ///////////////////////////////////////////////////////////////////////
        //  Public members

        /// <summary>
        ///     Creates a TriggerActionCollection
        /// </summary>
        public TriggerActionCollection()
        {
            _rawList = new List<TriggerAction>();
        }

        /// <summary>
        ///     Creates a TriggerActionCollection starting at the given size
        /// </summary>
        public TriggerActionCollection(int initialSize)
        {
            _rawList = new List<TriggerAction>(initialSize);
        }

        ///////////////////////////////////////////////////////////////////////
        // Public non-type-specific properties and methods that satisfy 
        //  implementation requirements of both IList and IList<T>

        /// <summary>
        ///     ICollection.Count
        /// </summary>
        public int Count
        {
            get
            {
                return _rawList.Count;
            }
        }

        /// <summary>
        ///     IList.IsReadOnly
        /// </summary>
        public bool IsReadOnly
        {
            get
            {
                return _sealed;
            }
        }

        /// <summary>
        ///     IList.Clear
        /// </summary>
        public void Clear()
        {
            CheckSealed();

            for (int i = _rawList.Count - 1; i >= 0; i--)
            {
                InheritanceContextHelper.RemoveContextFromObject(_owner, _rawList[i]);
            }

            _rawList.Clear();
        }

        /// <summary>
        ///     IList.RemoveAt
        /// </summary>
        public void RemoveAt(int index)
        {
            CheckSealed();
            TriggerAction oldValue = _rawList[index];
            InheritanceContextHelper.RemoveContextFromObject(_owner, oldValue);
            _rawList.RemoveAt(index);
            
        }

        ///////////////////////////////////////////////////////////////////////
        //  Strongly-typed implementations

        /// <summary>
        ///     IList.Add
        /// </summary>

        public void Add(TriggerAction value)
        {
            CheckSealed();
            InheritanceContextHelper.ProvideContextForObject( _owner, value );
            _rawList.Add(value);
        }


        /// <summary>
        ///     IList.Contains
        /// </summary>
        public bool Contains(TriggerAction value)
        {
            return _rawList.Contains(value);
        }

        /// <summary>
        ///     ICollection.CopyTo
        /// </summary>
        public void CopyTo( TriggerAction[] array, int index )
        {
            _rawList.CopyTo(array, index);
        }
        
        /// <summary>
        ///     IList.IndexOf
        /// </summary>
        public int IndexOf(TriggerAction value)
        {
            return _rawList.IndexOf(value);
        }

        /// <summary>
        ///     IList.Insert
        /// </summary>
        public void Insert(int index, TriggerAction value)
        {
            CheckSealed();
            InheritanceContextHelper.ProvideContextForObject(_owner, value );
            _rawList.Insert(index, value);

        }

        /// <summary>
        ///     IList.Remove
        /// </summary>
        public bool Remove(TriggerAction value)
        {
            CheckSealed();
            InheritanceContextHelper.RemoveContextFromObject(_owner, value);
            bool wasRemoved = _rawList.Remove(value);
            return wasRemoved;
        }

        /// <summary>
        ///     IList.Item
        /// </summary>
        public TriggerAction this[int index]
        {
            get
            {
                return _rawList[index];
            }
            set
            {
                CheckSealed();

                object oldValue = _rawList[index];
                InheritanceContextHelper.RemoveContextFromObject(Owner, oldValue as DependencyObject);
                _rawList[index] = value;
            }
        }

        /// <summary>
        ///     IEnumerable.GetEnumerator
        /// </summary>
        [CLSCompliant(false)]
        public IEnumerator<TriggerAction> GetEnumerator()
        {
            return _rawList.GetEnumerator();
        }

        ///////////////////////////////////////////////////////////////////////
        //  Object-based implementations that can be removed once Parser 
        //      has IList<T> support for strong typing.

        int IList.Add(object value)
        {
            CheckSealed();
            InheritanceContextHelper.ProvideContextForObject(_owner, value as DependencyObject);
            int index = ((IList) _rawList).Add(VerifyIsTriggerAction(value));
            return index;
        }

        bool IList.Contains(object value)
        {
            return _rawList.Contains(VerifyIsTriggerAction(value));
        }
        
        int IList.IndexOf(object value)
        {
            return _rawList.IndexOf(VerifyIsTriggerAction(value));
        }

        void IList.Insert(int index, object value)
        {
            Insert(index, VerifyIsTriggerAction(value));
        }

        bool IList.IsFixedSize
        {
            get
            {
                return _sealed;
            }
        }

        void IList.Remove(object value)
        {
            Remove(VerifyIsTriggerAction(value));
        }

        object IList.this[int index]
        {
            get
            {
                return _rawList[index];
            }
            set
            {
                this[index] = VerifyIsTriggerAction(value);
            }
        }

        void ICollection.CopyTo(Array array, int index)
        {
            ((ICollection)_rawList).CopyTo(array, index);
        }

        object ICollection.SyncRoot
        {
            get
            {
                return this;
            }
        }

        bool ICollection.IsSynchronized
        {
            get
            {
                return false;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_rawList).GetEnumerator();
        }

        ///////////////////////////////////////////////////////////////////////
        //  Internal members

        internal void Seal(TriggerBase containingTrigger )
        {
            for( int i = 0; i < _rawList.Count; i++ )
            {
                _rawList[i].Seal(containingTrigger);
            }
        }

        // The event trigger that we're in

        internal DependencyObject Owner
        {
            get { return _owner; }
            set
            { 
                Debug.Assert (Owner == null);
                _owner = value; 
            }
        }

        ///////////////////////////////////////////////////////////////////////
        //  Private members

        // Throw if a change is attempted at a time we're not allowing them
        private void CheckSealed()
        {
            if ( _sealed )
            {
                throw new InvalidOperationException(SR.Get(SRID.CannotChangeAfterSealed, "TriggerActionCollection"));
            }            
        }

        // Throw if the given object isn't a TriggerAction
        private TriggerAction VerifyIsTriggerAction(object value)
        {
            TriggerAction action = value as TriggerAction;

            if( action == null )
            {
                if( value == null )
                {
                    throw new ArgumentNullException("value");
                }
                else
                {
                    throw new ArgumentException(SR.Get(SRID.MustBeTriggerAction));
                }
            }

            return action;
        }



        // The actual underlying storage for our TriggerActions
        private List<TriggerAction> _rawList;

        // Whether we are allowing further changes to the collection
        private bool _sealed = false;

        // The event trigger that we're in
        private DependencyObject _owner = null;
        
    }
}
