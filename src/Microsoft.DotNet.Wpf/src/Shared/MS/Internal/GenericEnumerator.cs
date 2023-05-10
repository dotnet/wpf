// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//------------------------------------------------------------------------------
//
//
//                                             
//  File:       GenericEnumerator.cs
//------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Diagnostics;
using System.Windows;
using MS.Utility;

#if PRESENTATION_CORE
using SR=MS.Internal.PresentationCore.SR;
#else
using SR=System.Windows.SR;
#endif

namespace MS.Internal
{
    /// <summary>
    /// GenericEnumerator
    /// </summary>
    internal class GenericEnumerator : IEnumerator
    {
        #region Delegates

        internal delegate int GetGenerationIDDelegate();

        #endregion

        #region Constructors

        private GenericEnumerator()
        {
        }

        internal GenericEnumerator(IList array, GetGenerationIDDelegate getGenerationID)
        {
            _array     = array;
            _count     = _array.Count;
            _position  = -1;
            _getGenerationID = getGenerationID;
            _originalGenerationID = _getGenerationID();
        }

        #endregion

        #region Private

        private void VerifyCurrent()
        {
            if (   (-1 == _position)
                || (_position >= _count))
            {
                throw new InvalidOperationException(SR.Enumerator_VerifyContext);
            }
        }

        #endregion

        #region IEnumerator

        /// <summary>
        /// Returns the object at the current location of the key times list.
        /// Use the strongly typed version instead.
        /// </summary>
        object
            IEnumerator.Current
        {
            get
            {
                VerifyCurrent();

                return _current;
            }
        }

        /// <summary>
        /// Move to the next value in the key times list
        /// </summary>
        /// <returns>true if succeeded, false if at the end of the list</returns>
        public bool MoveNext()
        {
            if (_getGenerationID() != _originalGenerationID)
            {
                throw new InvalidOperationException(SR.Enumerator_CollectionChanged);
            }

            _position++;

            if (_position >= _count)
            {
                _position = _count;

                return false;
            }
            else
            {
                Debug.Assert(_position >= 0);

                _current = _array[_position];

                return true;
            }
        }

        /// <summary>
        /// Move to the position before the first value in the list.
        /// </summary>
        public void Reset()
        {
            if (_getGenerationID() != _originalGenerationID)
            {
                throw new InvalidOperationException(SR.Enumerator_CollectionChanged);
            }
            else
            {
                _position = -1;
            }
        }

        #endregion
        
        #region Data

        private IList   _array;
        private object  _current;
        private int     _count;
        private int     _position;
        private int     _originalGenerationID;

        private GetGenerationIDDelegate _getGenerationID;

        #endregion
    }
}

