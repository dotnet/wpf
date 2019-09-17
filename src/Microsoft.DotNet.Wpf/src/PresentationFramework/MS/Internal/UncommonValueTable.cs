// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: A table for storing up to 32 uncommon values.
//

using System;
using System.Windows;

namespace MS.Internal
{
    // An economical table for "uncommon values", identified by integers in the range [0,32).
    // Unused values incur no memory allocation

    internal struct UncommonValueTable
    {
        public bool HasValue(int id)
        {
            return (_bitmask & (0x1u << id)) != 0;
        }

        public object GetValue(int id)
        {
            return GetValue(id, DependencyProperty.UnsetValue);
        }

        public object GetValue(int id, object defaultValue)
        {
            int index = IndexOf(id);
            return (index < 0) ? defaultValue : _table[index];
        }

        public void SetValue(int id, object value)
        {
            int index = Find(id);
            if (index < 0)
            {
                // new entry - grow the array
                if (_table == null)
                {
                    _table = new object[1];
                    index = 0;
                }
                else
                {
                    int n = _table.Length;
                    object[] newTable = new object[n + 1];
                    index = ~index;

                    Array.Copy(_table, 0, newTable, 0, index);
                    Array.Copy(_table, index, newTable, index+1, n-index);
                    _table = newTable;
                }

                // mark the id as present
                _bitmask |= (0x1u << id);
            }

            // store the new value
            _table[index] = value;
        }

        public void ClearValue(int id)
        {
            int index = Find(id);
            if (index >= 0)
            {
                // remove the value
                int n = _table.Length - 1;
                if (n == 0)
                {
                    _table = null;
                }
                else
                {
                    object[] newTable = new object[n];
                    Array.Copy(_table, 0, newTable, 0, index);
                    Array.Copy(_table, index+1, newTable, index, n-index);
                    _table = newTable;
                }

                // mark the id as absent
                _bitmask &= ~(0x1u << id);
            }
        }

        // return the index within the table, -1 if not present
        private int IndexOf(int id)
        {
            return HasValue(id) ? GetIndex(id) : -1;
        }

        // return the index within the table, 1's complement if not present
        private int Find(int id)
        {
            int index = GetIndex(id);
            if (!HasValue(id))
            {
                index = ~index;
            }
            return index;
        }

        // get the index for the given id:  the number of 1-bits in _bitmask
        // to the right of the bit for the id.
        private int GetIndex(int id)
        {
            unchecked   // the multiplication in step 5 will overflow - we don't need the overflowing bits
            {
                // we count the bits in parallel, using 32-bit operations.  This
                // is an old technique - Knuth says it was known in the 1950's.
                // See The Art of Computer Programming 7.1.3-(62).
                // 1. Discard the bits at or above the given id
                uint x = (_bitmask << (31 - id)) << 1;      // (x<<32) is undefined
                // 2. Replace each 2-bit chunk with the count of 1's in that chunk
                x = x - ((x>>1) & 0x55555555u);
                // 3. Accumulate the counts within each 4-bit chunk
                x = (x & 0x33333333u) + ((x>>2) & 0x33333333u);
                // 4. Accumulate the counts within each 8-bit chunk (i.e. byte)
                x = (x + (x>>4)) & 0x0F0F0F0Fu;
                // 5. Sum the byte counts into the msb, and move the answer back to the lsb
                return (int) ((x * 0x01010101u) >> 24);

                // this method is often better than table-lookup methods, as it avoids
                // cache-misses on the table.   Everything happens in registers.
            }
        }

        private object[] _table;
        private uint _bitmask;
    }
}
