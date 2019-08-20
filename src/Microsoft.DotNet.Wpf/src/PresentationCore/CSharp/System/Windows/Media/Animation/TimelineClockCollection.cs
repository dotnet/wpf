// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// ClockCollection.cs

using System;
using System.Collections.Generic;
using System.Diagnostics;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

#pragma warning disable 1634, 1691  // suppressing PreSharp warnings

namespace System.Windows.Media.Animation
{
    /// <summary>
    /// A collection of Clock objects.
    /// </summary>
    public class ClockCollection : ICollection<Clock>
    {
        #region External interface

        #region ICollection

        #region Properties

        /// <summary>
        /// Gets the number of elements contained in the collection.
        /// </summary>
        /// <value>
        /// The number of elements contained in the collection.
        /// </value>
        public int Count
        {
            get
            {
//                 _owner.VerifyAccess();
                ClockGroup clockGroup = _owner as ClockGroup;
                if (clockGroup != null)
                {
                    List<Clock> childList = clockGroup.InternalChildren;
                    if (childList != null)
                    {
                        return childList.Count;
                    }
                }
                return 0;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <value></value>
        public bool IsReadOnly
        {
            get
            {
                return true;
            }
        }

        #endregion // Properties

        #region Methods

        /// <summary>
        /// 
        /// </summary>
        public void Clear()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public void Add(Clock item)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Remove(Clock item)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Contains(Clock item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            foreach (Clock t in this)
            {
                #pragma warning suppress 6506 // the enumerator will not contain nulls
                if (t.Equals(item))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Copies the elements of the collection to an array, starting at a
        /// particular array index.
        /// </summary>
        /// <param name="array">
        /// The one-dimensional array that is the destination of the elements
        /// copied from the collection. The Array must have zero-based indexing.
        /// </param>
        /// <param name="index">
        /// The zero-based index in array at which copying begins.
        /// </param>
        public void CopyTo(Clock[] array, int index)
        {
//             _owner.VerifyAccess();

            ClockGroup clockGroup = _owner as ClockGroup;

            if (clockGroup != null)
            {
                List<Clock> list = clockGroup.InternalChildren;

                if (list != null)
                {
                    // Get free parameter validation from Array.Copy
                    list.CopyTo(array, index);
                }
            }

            // Need to perform parameter validation in the list == null case
        }

        #endregion // Methods

        #endregion // ICollection

        #region IEnumerable

        #region Methods

        /// <summary>
        /// Returns an enumerator that can iterate through a collection.
        /// </summary>
        /// <returns>
        /// An enumerator that can iterate through a collection.
        /// </returns>
        IEnumerator<Clock> IEnumerable<Clock>.GetEnumerator()
        {
//             _owner.VerifyAccess();

            List<Clock> list = null;
            ClockGroup clockGroup = _owner as ClockGroup;

            if (clockGroup != null)
            {
                list = clockGroup.InternalChildren;
            }

            if (list != null)
            {
                return list.GetEnumerator();
            }
            else
            {
                return new ClockEnumerator(_owner);
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new ClockEnumerator(_owner);
        }

        /// <summary>
        /// Checks for equality of two ClockCollections
        /// </summary>
        /// <param name="obj">
        /// Other object against which to check for equality
        /// </param>
        public override bool Equals(object obj)
        {
            if (obj is ClockCollection)
            {
                return (this == (ClockCollection)obj);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Checks for equality of two ClockCollections
        /// </summary>
        /// <param name="objA">
        /// First ClockCollection to check for equality
        /// </param>
        /// <param name="objB">
        /// Second ClockCollection to check for equality
        /// </param>
        public static bool Equals(ClockCollection objA, ClockCollection objB)
        {
            return (objA == objB);
        }

        /// <summary>
        /// Shallow comparison for equality: A and B have the same owner
        /// </summary>
        /// <param name="objA">
        /// First ClockCollection to check for equality
        /// </param>
        /// <param name="objB">
        /// Second ClockCollection to check for equality
        /// </param>
        public static bool operator ==(ClockCollection objA, ClockCollection objB)
        {
            if (Object.ReferenceEquals(objA, objB))
            {
                // Exact same object.
                return true;
            }
            else if (   Object.ReferenceEquals(objA, null)
                     || Object.ReferenceEquals(objB, null))
            {
                // One is null, the other isn't.
                return false;
            }
            else
            {
                // Both are non-null.
#pragma warning disable 56506 // Suppress presharp warning: Parameter 'objA' to this public method must be validated:  A null-dereference can occur here.
                return objA._owner == objB._owner;
#pragma warning restore 56506
            }
        }

        /// <summary>
        /// Shallow comparison for inequality: A and B have different owner
        /// </summary>
        /// <param name="objA">
        /// First ClockCollection to check for inequality
        /// </param>
        /// <param name="objB">
        /// Second ClockCollection to check for inequality
        /// </param>
        public static bool operator !=(ClockCollection objA, ClockCollection objB)
        {
            return !(objA == objB);
        }

        ///<summary>
        /// GetHashCode
        ///</summary>
        public override int GetHashCode()
        {
            return _owner.GetHashCode();
        }

        #endregion // Methods

        #endregion // IEnumerable

        #region Properties

        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        /// <value>
        /// The element at the specified index.
        /// </value>
        public Clock this[int index]
        {
            get
            {
//                 _owner.VerifyAccess();

                List<Clock> list = null;
                ClockGroup clockGroup = _owner as ClockGroup;

                if (clockGroup != null)
                {
                    list = clockGroup.InternalChildren;
                }

                if (list == null)
                {
                    throw new ArgumentOutOfRangeException("index");
                }

                return list[index];
            }
        }

        #endregion // Properties

        #endregion // External interface

        #region Internal implementation

        #region Types

        /// <summary>
        /// An enumerator for a ClockCollection object.
        /// </summary>
        internal struct ClockEnumerator : IEnumerator<Clock>
        {
            #region Construction

            /// <summary>
            /// Creates an enumerator for the specified clock.
            /// </summary>
            /// <param name="owner">
            /// The clock whose children to enumerate.
            /// </param>
            internal ClockEnumerator(Clock owner)
            {
                _owner = owner;
            }

            #endregion // Construction

            #region IDisposable interface

            /// <summary>
            /// Disposes the enumerator.
            /// </summary>
            public void Dispose()
            {
                // The enumerator doesn't do much, so we don't have to do
                // anything to dispose it.
            }

            #endregion // IDisposable interface

            #region IEnumerator interface

            /// <summary>
            /// Gets the current element in the collection.
            /// </summary>
            /// <value>
            /// The current element in the collection.
            /// </value>
            Clock IEnumerator<Clock>.Current
             {
                get
                {
                    throw new InvalidOperationException(SR.Get(SRID.Timing_EnumeratorOutOfRange));
                }
            }

            #region IEnumerator Members

            object System.Collections.IEnumerator.Current
            {
                get
                {
                    return ((IEnumerator<Clock>)this).Current;
                }
            }

            void System.Collections.IEnumerator.Reset()
            {
                throw new NotImplementedException();
            }

            #endregion

            /// <summary>
            /// Advances the enumerator to the next element of the collection.
            /// </summary>
            /// <returns>
            /// true if the enumerator was successfully advanced to the next
            /// element; false if the enumerator has passed the end of the
            /// collection.
            /// </returns>
            public bool MoveNext()
            {
                // If the collection is no longer empty, it means it was
                // modified and we should thrown an exception. Otherwise, we
                // are still valid, but the collection is empty so we should
                // just return false.

//                 _owner.VerifyAccess();

                ClockGroup clockGroup = _owner as ClockGroup;

                if (clockGroup != null && clockGroup.InternalChildren != null)
                {
                    throw new InvalidOperationException(SR.Get(SRID.Timing_EnumeratorInvalidated));
                }

                return false;
            }

            #endregion // IEnumerator interface

            #region Internal implementation

            #region Data

            private Clock   _owner;

            #endregion // Data

            #endregion // Internal implementation
        }

        #endregion // Types

        #region Constructors

        /// <summary>
        /// Creates an initially empty collection of Clock objects.
        /// </summary>
        /// <param name="owner">
        /// The Clock that owns this collection.
        /// </param>
        internal ClockCollection(Clock owner)
        {
            Debug.Assert(owner != null, "ClockCollection must have a non-null owner.");
            _owner = owner;
        }

        /// <summary>
        /// Disallow parameterless constructors
        /// </summary>
        private ClockCollection()
        {
            Debug.Assert(false, "Parameterless constructor is illegal for ClockCollection.");
        }

        #endregion

        #region Data

        private Clock       _owner;

        #endregion // Data

        #endregion // Internal implementation
    }
}

