// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//------------------------------------------------------------------------------
//                                             
//  File:       TimelineEnumerator.cs
//------------------------------------------------------------------------------

// Allow suppression of certain presharp messages
#pragma warning disable 1634, 1691

using System;
using System.Collections;
using MS.Internal;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media.Animation
{
    /// <summary>
    /// An enumerator that iterates over the children of a timeline.
    /// </summary>
    internal struct TimelineEnumerator : IEnumerator
    {
        #region External interface

        #region IEnumerator interface

        #region Properties

        /// <summary>
        /// Gets the current element in the collection.
        /// </summary>
        /// <value>
        /// The current element in the collection.
        /// </value>
        object IEnumerator.Current
        {
            get
            {
                return Current;
            }
        }

        #endregion // Properties

        #region Methods

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>
        /// True if the enumerator was successfully advanced to the next element;
        /// false if the enumerator has passed the end of the collection.
        /// </returns>
        public bool MoveNext()
        {
            VerifyVersion();
            if (_currentIndex < _owner.Count - 1)
            {
                _currentIndex++;
                return true;
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// Sets the enumerator to its initial position, which is before the first element
        /// in the collection.
        /// </summary>
        public void Reset()
        {
            VerifyVersion();
            _currentIndex = -1;
        }

        #endregion // Methods

        #endregion // IEnumerator interface

        #region Properties
        /// <summary>
        /// The current timeline referenced by this enumerator.
        /// </summary>
        public Timeline Current
        {
            get
            {
                VerifyVersion();
                if (_currentIndex < 0 || _currentIndex == _owner.Count)
                {
#pragma warning suppress 56503 // Suppress presharp warning: Follows a pattern similar to Nullable.
                    throw new InvalidOperationException(SR.Get(SRID.Timing_EnumeratorOutOfRange));
                }

                return _owner[_currentIndex];
            }
        }
        #endregion // Properties

        #endregion // External interface

        #region Internal implementation

        #region Construction

        /// <summary>
        /// Creates an enumerator iterates over the children of the specified container.
        /// </summary>
        /// <param name="owner">
        /// The collection we are enumerating.
        /// </param>
        internal TimelineEnumerator(TimelineCollection owner)
        {
            _owner = owner;
            _currentIndex = -1;
            _version = _owner.Version;
        }

        #endregion // Construction

        #region Methods

        /// <summary>
        /// Verifies that the enumerator is still valid by comparing its initial version
        /// with the current version of the collection.
        /// </summary>
        private void VerifyVersion()
        {
            if (_version != _owner.Version)
            {
                throw new InvalidOperationException(SR.Get(SRID.Timing_EnumeratorInvalidated));
            }
        }

        #endregion // Methods

        #region Data

        private TimelineCollection   _owner;
        private int                     _currentIndex;
        private int                     _version;

        #endregion // Data

        #endregion // Internal implementation
    }
}
