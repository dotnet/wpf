// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//---------------------------------------------------------------------------
//

// 
//
// Description: A class that implements ICollection<ushort> for a sequence of numbers [0..n-1].
// 
//
//  
//
//
//---------------------------------------------------------------------------

using System;
using System.Windows;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Runtime.InteropServices;

using SR=MS.Internal.PresentationCore.SR;

namespace MS.Internal
{
    internal class SequentialUshortCollection : ICollection<ushort>
    {
        public SequentialUshortCollection(ushort count)
        {
            _count = count;
        }

        #region ICollection<ushort> Members

        public void Add(ushort item)
        {
            throw new NotSupportedException();
        }

        public void Clear()
        {
            throw new NotSupportedException();
        }

        public bool Contains(ushort item)
        {
            return item < _count;
        }

        public void CopyTo(ushort[] array, int arrayIndex)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }

            if (array.Rank != 1)
            {
                throw new ArgumentException(SR.Collection_BadRank);
            }

            // The extra "arrayIndex >= array.Length" check in because even if _collection.Count
            // is 0 the index is not allowed to be equal or greater than the length
            // (from the MSDN ICollection docs)
            if (arrayIndex < 0 || arrayIndex >= array.Length || (arrayIndex + Count) > array.Length)
            {
                throw new ArgumentOutOfRangeException("arrayIndex");
            }

            for (ushort i = 0; i < _count; ++i)
                array[arrayIndex + i] = i;
        }

        public int Count
        {
            get { return _count; }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public bool Remove(ushort item)
        {
            throw new NotSupportedException();
        }

        #endregion

        #region IEnumerable<ushort> Members

        public IEnumerator<ushort> GetEnumerator()
        {
            for (ushort i = 0; i < _count; ++i)
                yield return i;
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<ushort>)this).GetEnumerator();
        }

        #endregion

        private ushort _count;
    }
}

