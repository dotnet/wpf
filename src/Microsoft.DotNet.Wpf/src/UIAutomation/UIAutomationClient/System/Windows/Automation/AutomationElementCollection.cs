// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Read-only collection of AutomationElements - effectively a
// wrapper for Array

using System;
using System.Collections;
using MS.Internal.Automation;

namespace System.Windows.Automation
{
    /// <summary>
    /// A read-only collection of AutomationElement objects
    /// </summary>
#if (INTERNAL_COMPILE)
    internal class AutomationElementCollection: ICollection
#else
    public class AutomationElementCollection: ICollection
#endif
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        internal AutomationElementCollection(AutomationElement[] elements)
        {
            _elements = elements;
        }

        #endregion Constructors


        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------
 
        #region Public Properties

        /// <summary>
        /// Returns the specified item in this collection
        /// </summary>
        public AutomationElement this[int index]
        {
            get
            {
                return _elements[ index ];
            }
        }

        #endregion Public Properties



        //------------------------------------------------------
        //
        //  Interface ICollection
        //
        //------------------------------------------------------
 
        #region Interface ICollection

        /// <summary>
        /// Copies all the elements of the current collection to the specified one-dimensional Array.
        /// </summary>
        public virtual void CopyTo( Array array, int index )
        {
            _elements.CopyTo( array, index );
        }

        /// <summary>
        /// Copies all the elements of the current collection to the specified one-dimensional Array.
        /// </summary>
        public void CopyTo(AutomationElement[] array, int index)
        {
            ((ICollection)this).CopyTo(array, index);
        }

        /// <summary>
        /// Returns the number of elements in this collection
        /// </summary>
        public int Count
        {
            get
            {
                return _elements.Length;
            }
        }

        /// <summary>
        /// Gets an object that can be used to synchronize access to the collection.
        /// </summary>
        public virtual Object SyncRoot
        {
            get
            {
                // Don't return _elements.SyncRoot, since that may leak a reference to the array,
                // allowing it to be modified.
                return this;
            }
        }

        /// <summary>
        /// Gets a value indicating whether access to the collection is synchronized (thread-safe).
        /// </summary>
        public virtual bool IsSynchronized
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Returns an IEnumerator for the collection
        /// </summary>
        public IEnumerator GetEnumerator()
        {
            return _elements.GetEnumerator();
        }

        #endregion Interface ICollection


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields

        private AutomationElement[] _elements;

        #endregion Private Fields
    }
}
