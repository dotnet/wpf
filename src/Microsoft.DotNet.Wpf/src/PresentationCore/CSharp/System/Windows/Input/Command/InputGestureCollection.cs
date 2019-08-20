// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: InputGestureCollection serves the purpose of Storing/Retrieving InputGestures 
//
//              See spec at : http://avalon/coreui/Specs/Commanding(new).mht
// 
//

using System;
using System.Collections;
using System.Diagnostics;
using System.Collections.Generic;
using System.Windows;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Input
{
    /// <summary>
    /// InputGestureCollection
    /// InputGestureCollection - Collection of InputGestures. 
    ///     Stores the InputGestures sequentially in a genric InputGesture List
    ///     Will be changed to generic List implementation once the 
    ///     parser supports generic collections.
    /// </summary>
    public sealed class InputGestureCollection : IList 
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
#region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        public InputGestureCollection() 
        {
        }

        /// <summary>
        /// InputGestureCollection
        /// </summary>
        /// <param name="inputGestures">InputGesture array</param>
        public InputGestureCollection( IList inputGestures )
        {
            if (inputGestures != null && inputGestures.Count > 0)
            {
                this.AddRange(inputGestures as ICollection);
            }
}
#endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

#region Public Methods

#region Implementation of IList 

#region Implementation of ICollection
        /// <summary>
        /// CopyTo - to copy the entire collection into an array
        /// </summary>
        /// <param name="array"></param>
        /// <param name="index"></param>
        void ICollection.CopyTo(System.Array array, int index) 
        {
            if (_innerGestureList != null)
                ((ICollection)_innerGestureList).CopyTo(array, index);
        }
     
#endregion Implementation of ICollection
        /// <summary>
        /// IList.Contains
        /// </summary>
        /// <param name="key">key</param>
        /// <returns>true - if found, false - otherwise</returns>
        bool IList.Contains(object key) 
        {
            return this.Contains(key as InputGesture) ;
        }

        /// <summary>
        /// IndexOf
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        int IList.IndexOf(object value)
        {
            InputGesture inputGesture = value as InputGesture;
            return ((inputGesture != null) ? this.IndexOf(inputGesture) : -1) ;
        }

        /// <summary>
        ///  Insert
        /// </summary>
        /// <param name="index">index at which to insert the item</param>
        /// <param name="value">item value to insert</param>
        void IList.Insert(int index, object value)
        {
            if (IsReadOnly)
                 throw new NotSupportedException(SR.Get(SRID.ReadOnlyInputGesturesCollection));    

            this.Insert(index, value as InputGesture);
        }

        /// <summary>
        /// Add
        /// </summary>
        /// <param name="inputGesture"></param>
        int IList.Add(object inputGesture) 
        {
            if (IsReadOnly)
                throw new NotSupportedException(SR.Get(SRID.ReadOnlyInputGesturesCollection));
            
            return this.Add(inputGesture as InputGesture);
        }

        /// <summary>
        /// Remove
        /// </summary>
        /// <param name="inputGesture"></param>
        void IList.Remove(object inputGesture) 
        {
            if (IsReadOnly)
                throw new NotSupportedException(SR.Get(SRID.ReadOnlyInputGesturesCollection));
            
            this.Remove(inputGesture as InputGesture);
        }

        /// <summary>
        /// Indexing operator
        /// </summary>
        object IList.this[int index]
        {
            get
            { 
                return this[index]; 
            }
            set 
            {
                InputGesture inputGesture = value as InputGesture;
                if (inputGesture == null)
                    throw new NotSupportedException(SR.Get(SRID.CollectionOnlyAcceptsInputGestures));

                this[index] = inputGesture;
            }
        }
#endregion Implementation of IList 

#region Implementation of Enumerable
         /// <summary>
         /// IEnumerable.GetEnumerator - For Enumeration purposes
         /// </summary>
         /// <returns></returns>
         public IEnumerator GetEnumerator()
         {
             if (_innerGestureList != null)
                 return _innerGestureList.GetEnumerator();

             List<InputGesture> list = new List<InputGesture>(0);
             return list.GetEnumerator();
         }
#endregion Implementation of IEnumberable
         /// <summary>
         /// Indexing operator
         /// </summary>
         public InputGesture this[int index]
         {
             get
             {
                 return (_innerGestureList != null ? _innerGestureList[index] : null);
             }
             set
             {
                 if (IsReadOnly)
                     throw new NotSupportedException(SR.Get(SRID.ReadOnlyInputGesturesCollection));

                 EnsureList();

                 if (_innerGestureList != null)
                 {
                     _innerGestureList[index] = value;
                 }
             }
         }                

         /// <summary>
         /// ICollection.IsSynchronized 
         /// </summary>
         public bool IsSynchronized
         {
             get
             {
                 if (_innerGestureList != null)
                     return ((IList)_innerGestureList).IsSynchronized;

                 return false;
             }
         }

         /// <summary>
         /// ICollection.SyncRoot
         /// </summary>
         public object SyncRoot
         {
             get
             {
                 return _innerGestureList != null ? ((IList)_innerGestureList).SyncRoot : this;
             }
         }

         /// <summary>
         /// IndexOf
         /// </summary>
         /// <param name="value"></param>
         /// <returns></returns>
         public int IndexOf(InputGesture value)
         {
             return (_innerGestureList != null) ? _innerGestureList.IndexOf(value) : -1;
         }

         /// <summary>
         /// RemoveAt - Removes the item at given index
         /// </summary>
         /// <param name="index">index at which item needs to be removed</param>
         public void RemoveAt(int index)
         {
             if (IsReadOnly)
                 throw new NotSupportedException(SR.Get(SRID.ReadOnlyInputGesturesCollection));

             if (_innerGestureList != null)
                _innerGestureList.RemoveAt(index);
         }

         /// <summary>
         /// IsFixedSize - Fixed Capacity if ReadOnly, else false.
         /// </summary>
         public bool IsFixedSize
         {
             get
             {
                 return IsReadOnly; 
             }
        }

        /// <summary>
        /// Add
        /// </summary>
        /// <param name="inputGesture"></param>
        public int Add(InputGesture inputGesture) 
        {
            if (IsReadOnly)
            {
                throw new NotSupportedException(SR.Get(SRID.ReadOnlyInputGesturesCollection));
            }

	    if (inputGesture == null)
            {
		throw new ArgumentNullException("inputGesture");
            }

            EnsureList();
            _innerGestureList.Add(inputGesture);
            return 0; // ICollection.Add no longer returns the indice
        }

        /// <summary>    
        /// Adds the elements of the given collection to the end of this list. If
        /// required, the capacity of the list is increased to twice the previous
        /// capacity or the new size, whichever is larger.
        /// </summary>
        /// <param name="collection">collection to append</param>
        public void AddRange(ICollection collection) 
        {
            if (IsReadOnly)
            {
                throw new NotSupportedException(SR.Get(SRID.ReadOnlyInputGesturesCollection));
            }

            if (collection == null)
                throw new ArgumentNullException("collection");
            
            if( collection.Count > 0) 
            {
                if (_innerGestureList == null)
                    _innerGestureList = new System.Collections.Generic.List<InputGesture>(collection.Count);

                IEnumerator collectionEnum = collection.GetEnumerator();
                while(collectionEnum.MoveNext()) 
                {
                    InputGesture inputGesture = collectionEnum.Current as InputGesture;
                    if (inputGesture != null)
                    {
                        _innerGestureList.Add(inputGesture);
                    }
                    else
                    {
                        throw new NotSupportedException(SR.Get(SRID.CollectionOnlyAcceptsInputGestures));
		    }
                }
            }
        }

        /// <summary>
        ///  Insert
        /// </summary>
        /// <param name="index">index at which to insert the item</param>
        /// <param name="inputGesture">item value to insert</param>
        public void Insert(int index, InputGesture inputGesture)
        {
            if (IsReadOnly)
                 throw new NotSupportedException(SR.Get(SRID.ReadOnlyInputGesturesCollection));    

            if (inputGesture == null)
                throw new NotSupportedException(SR.Get(SRID.CollectionOnlyAcceptsInputGestures));

            if (_innerGestureList != null)
                _innerGestureList.Insert(index, inputGesture);
        }

        /// <summary>
        /// IsReadOnly - Tells whether this is readonly Collection.
        /// </summary>
        public bool IsReadOnly
        {
            get 
            {
                return (_isReadOnly);
            }
        }
        
        /// <summary>
        /// Remove
        /// </summary>
        /// <param name="inputGesture"></param>
        public void Remove(InputGesture inputGesture) 
        {
            if (IsReadOnly)
            {
                 throw new NotSupportedException(SR.Get(SRID.ReadOnlyInputGesturesCollection));
            }

            if (inputGesture == null)
 	        {
                throw new ArgumentNullException("inputGesture");
	        }

            if (_innerGestureList != null && _innerGestureList.Contains(inputGesture))
            {
                _innerGestureList.Remove(inputGesture as InputGesture);
            }
        }

        /// <summary>
        /// Count
        /// </summary>
        public int Count 
        {
            get 
            {
                return (_innerGestureList != null ? _innerGestureList.Count : 0 );
            }
        }

        /// <summary>
        /// Clears the Entire InputGestureCollection
        /// </summary>
        public void Clear()
        {
            if (IsReadOnly)
            {
                 throw new NotSupportedException(SR.Get(SRID.ReadOnlyInputGesturesCollection));
            }
         
	    if (_innerGestureList != null)
            {
               _innerGestureList.Clear();
               _innerGestureList = null;
            }
        }

        /// <summary>
        /// Contains
        /// </summary>
        /// <param name="key">key</param>
        /// <returns>true - if found, false - otherwise</returns>
        public bool Contains(InputGesture key) 
        {
            if (_innerGestureList != null && key != null)
            {
               return _innerGestureList.Contains(key) ;
            }
            return false;
        }

        /// <summary>
        /// CopyTo - to copy the collection starting from given index into an array
        /// </summary>
        /// <param name="inputGestures">Array of InputGesture</param>
        /// <param name="index">start index of items to copy</param>
        public void CopyTo(InputGesture[] inputGestures, int index) 
        {
            if (_innerGestureList != null)
                _innerGestureList.CopyTo(inputGestures, index);
        }

        /// <summary>
        /// Seal the Collection by setting it as  read-only.
        /// </summary>
        public void Seal()
        {
            _isReadOnly = true;
        }

        private void EnsureList()
        {
            if (_innerGestureList == null)
                _innerGestureList = new List<InputGesture>(1);
        }
#endregion Public

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
        #region Internal

        internal InputGesture FindMatch(object targetElement, InputEventArgs inputEventArgs)
        {
            for (int i = 0; i < Count; i++)
            {
                InputGesture inputGesture = this[i];
                if (inputGesture.Matches(targetElement, inputEventArgs))
                {
                    return inputGesture;
                }
            }

            return null;
        }

        #endregion

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
#region Private Fields
        private System.Collections.Generic.List<InputGesture> _innerGestureList;
        private bool           _isReadOnly = false;
#endregion Private Fields
    }
}
