// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Windows.Media.Animation
{
    /// <summary>
    /// An enumerator for collections of weak references.
    /// </summary>
    /// <remarks>
    /// The enumerator returns only live objects and removes dead
    /// references from the list as it walks.
    /// </remarks>
    internal struct WeakRefEnumerator<T>
    {
        #region Internal implementation

        #region Construction

        /// <summary>
        /// Creates an enumerator for the given list.
        /// </summary>
        /// <param name="list">
        /// The list to enumerate.
        /// </param>
        internal WeakRefEnumerator(List<WeakReference> list)
        {
            _list = list;
            _readIndex = 0;
            _writeIndex = 0;
            _current = default(T);

#if DEBUG
            _valid = false;
#endif // DEBUG
        }

        #endregion // Construction

        #region Properties

        /// <summary>
        /// Gets the current element in the collection.
        /// </summary>
        /// <value>
        /// The current element in the collection.
        /// </value>
        internal T Current
        {
            get
            {
#if DEBUG
                Debug.Assert(_valid);
#endif // DEBUG

                return _current;
            }
        }

        /// <summary>
        /// Gets the index of the current element in the collection.
        /// </summary>
        /// <value>
        /// The index of the current element in the collection.
        /// </value>
        internal int CurrentIndex
        {
            get
            {
#if DEBUG
                Debug.Assert(_valid);
#endif // DEBUG

                return _writeIndex - 1;
            }
        }

        #endregion // Properties

        #region Methods

        /// <summary>
        /// This method must be called when the enumerator is no longer
        /// in use, if the last call to MoveNext returned true. The
        /// enumerator is no longer valid after this call.
        /// </summary>
        /// <remarks>
        /// When MoveNext returns false, the collection being enumerated is
        /// left in a good, known state, so nothing additional needs to be
        /// done. However, before MoveNext returns false the collection is
        /// in an intermediate state, so if the enumerator isn't advanced
        /// all the way to the end then this method must be called to clean
        /// up the collection and make sure that it ends in a known state.
        /// Calling this method in the case where MoveNext has returned
        /// false is OK. In that case, the method is a no-op.
        /// </remarks>
        internal void Dispose()
        {
            // Remove only those elements that we've previously seen.
            // This means removing the range between the read and write
            // indices, as that's dead space in the collection.
            if (_readIndex != _writeIndex)
            {
                _list.RemoveRange(_writeIndex, _readIndex - _writeIndex);
                _readIndex = _writeIndex = _list.Count;
            }

            _current = default(T);

#if DEBUG
            _valid = false;
#endif // DEBUG
        }

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>
        /// true if the enumerator was successfully advanced to the next
        /// element; false if the enumerator has passed the end of the
        /// collection.
        /// </returns>
        /// <remarks>
        /// Elements that have been GC'ed are removed from the list.
        /// </remarks>
        internal bool MoveNext()
        {
            // Get a reference to the next element that has not yet been
            // garbage-collected
            while (_readIndex < _list.Count)
            {
                WeakReference currentRef = _list[_readIndex];
                _current = (T)currentRef.Target;
                if ( (object)_current != null)
                {
                    // Found a live object. First compress the list, which
                    // is necessary if we've previously seen GC'ed objects.
                    if (_writeIndex != _readIndex)
                    {
                        _list[_writeIndex] = currentRef;
                    }

                    // Update internal state and return the found element
                    _readIndex++;
                    _writeIndex++;

#if DEBUG
                    _valid = true;
#endif // DEBUG

                    return true;
                }
                else
                {
                    // This object was garbage-collected, so keep looking
                    _readIndex++;
                }
            }

            // If we get here we didn't have any more live elements in the
            // collection, so we should return false. This is also a good
            // opportunity to clean up the list as necessary.
            Dispose();

            return false;
        }


        #endregion // Methods

        #region Data

        private List<WeakReference> _list;
        private T                   _current;
        private int                 _readIndex;
        private int                 _writeIndex;

#if DEBUG
        private bool                _valid;
#endif // DEBUG

        #endregion // Data

        #endregion // Internal implementation
    }
}
