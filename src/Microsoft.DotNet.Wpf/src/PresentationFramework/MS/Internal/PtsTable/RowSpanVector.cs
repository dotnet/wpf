// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
// Description: Implementation of Row Span Vector. 
//


using System;
using System.Diagnostics;
using System.Windows.Documents;

namespace MS.Internal.PtsTable
{
    /// <summary>
    /// Implementation of Row Span Vector. 
    /// </summary>
    /// <remarks>
    /// Each row span cell in a table goes through row span vector. 
    /// RowSpanVector play several roles:
    /// * it transfers information about row spanning cells from a row 
    ///   to the next row during structural cache validation;
    /// * it provides information about available ranges, in which cells 
    ///   are positioned;
    /// * at the end of row validation RowSpanVector prepares array of 
    ///   row spanned cells that start, end or go through the row;
    /// </remarks>
    internal sealed class RowSpanVector
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Default constructor
        /// </summary>
        internal RowSpanVector()
        {
            _entries = new Entry[c_defaultCapacity];

            // add the barrier element
            _entries[0].Cell = null;
            _entries[0].Start = int.MaxValue / 2;
            _entries[0].Range = int.MaxValue / 2;
            _entries[0].Ttl = int.MaxValue;
            _size = 1;

            #if DEBUG
            _index = -1;
            #endif // DEBUG
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods 

        /// <summary>
        /// Registers the cell by creating a dirty range and inserting it into 
        /// existing dirty range list.
        /// </summary>
        /// <param name="cell">Reference to cell</param>
        internal void Register(TableCell cell)
        {
            int start = cell.ColumnIndex;

            #if DEBUG
            Debug.Assert(cell != null
                        &&  cell.ColumnIndex != -1
                        &&  cell.RowSpan > 1
                        &&  _index != -1    );

            // assert there is no register record with this index
            for (int i = _size - 2; i >= 0; --i)
            {
                Debug.Assert(   start < _entries[i].Cell.ColumnIndex
                            ||  start >= _entries[i].Cell.ColumnIndex + _entries[i].Cell.ColumnSpan);
            }

            //  assert that the position for the element being inserted 
            //  is correct 
            Debug.Assert(_index < _size);
            Debug.Assert(_index == 0 || _entries[_index - 1].Start < start);
            Debug.Assert(start < _entries[_index].Start);
#endif // DEBUG

            //  check if array of entries has enough capacity to hold another entry
            if (_size == _entries.Length)
            {
                InflateCapacity();
            }

            //  insert
            for (int i = _size - 1; i >= _index; --i)
            {
                _entries[i + 1] = _entries[i];
            }

            _entries[_index].Cell = cell;
            _entries[_index].Start = start;
            _entries[_index].Range = cell.ColumnSpan;
            _entries[_index].Ttl = cell.RowSpan - 1;
            _size++;
            _index++;
        }

        /// <summary>
        /// Returns the first empty range of indices
        /// </summary>
        /// <param name="firstAvailableIndex">First availalbe index</param>
        /// <param name="firstOccupiedIndex">First occupied index</param>
        internal void GetFirstAvailableRange(out int firstAvailableIndex, out int firstOccupiedIndex)
        {
            _index = 0;
            firstAvailableIndex = 0;
            firstOccupiedIndex = _entries[_index].Start;
        }

        /// <summary>
        /// Returns the next empty range of indices
        /// </summary>
        /// <param name="firstAvailableIndex">First availalbe index</param>
        /// <param name="firstOccupiedIndex">First occupied index</param>
        /// <remarks>
        /// Side effect: updates ttl counter
        /// </remarks>
        internal void GetNextAvailableRange(out int firstAvailableIndex, out int firstOccupiedIndex)
        {
            //  calculate first available index
            Debug.Assert(0 <= _index && _index < _size);
            firstAvailableIndex = _entries[_index].Start + _entries[_index].Range;
            
            //  update ttl counter
            _entries[_index].Ttl--;
            
            //  calculate first occupied index
            _index++;
            Debug.Assert(0 <= _index && _index < _size);
            firstOccupiedIndex = _entries[_index].Start;
        }

        /// <summary>
        /// Returns array of spanned cells
        /// </summary>
        /// <param name="cells">Spanned cells</param>
        /// <param name="isLastRowOfAnySpan">Whether the current span has the last row of any span</param>
        /// <returns>Array of cells. May be empty</returns>
        internal void GetSpanCells(out TableCell[] cells, out bool isLastRowOfAnySpan)
        {
            cells = s_noCells;
            isLastRowOfAnySpan = false;

            //  iterate the tail of entries (if any) 
            //  update ttl counter
            while (_index < _size)
            {
                _entries[_index].Ttl--;
                _index++;
            }

            //  * copy surviving entries (if any) into array
            //  * remove expired entries
            if (_size > 1)
            {
                cells = new TableCell[_size - 1];

                int i = 0, j = 0;

                do
                {
                    Debug.Assert(_entries[i].Cell != null);
                    Debug.Assert(i >= j);
                    
                    cells[i] = _entries[i].Cell;
                    
                    if (_entries[i].Ttl > 0)
                    {
                        if (i != j)
                        {
                            _entries[j] = _entries[i];
                        }
                    
                        j++;
                    }
                    
                    i++;
                } while (i < _size - 1);
                
                //  take care of the barrier entry
                if (i != j)
                {
                    _entries[j] = _entries[i];
                    isLastRowOfAnySpan = true;
                }
                
                _size = j + 1;
            }

            #if DEBUG
            _index = -1;
            #endif // DEBUG
        }

        #endregion Internal Methods 

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties 

        /// <summary>
        /// Returns "true" when there is no registered cells in the vector
        /// </summary>
        /// <returns>Returns "true" when there is no registered cells in the vector</returns>
        internal bool Empty()
        {
            return (_size == 1);
        }

        #endregion Internal Properties 

        //------------------------------------------------------
        //
        //  Private Methods 
        //
        //------------------------------------------------------

        #region Private Methods 

        /// <summary>
        /// Increases capacity of the internal array by the factor of 2 
        /// </summary>
        private void InflateCapacity()
        {
            Debug.Assert(   _entries.Length > 0
                        &&  _size <= _entries.Length    );

            Entry[] newEntries = new Entry[_entries.Length * 2];
            Array.Copy(_entries, newEntries, _entries.Length);
            _entries = newEntries;
        }

        #endregion Private Methods 

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields 
        private Entry[] _entries;                       //  dirty range list
        private int _size;                              //  current size of the list
        private int _index;                             //  index used for iteration (GetFirst / GetNext)
        private const int c_defaultCapacity = 8;        //  default capacity
        private static TableCell[] s_noCells = new TableCell[0];  //  empty array RowSpanVector returns to rows that do not 
                                                        //  have row spanned cells
        #endregion Private Fields 

        //------------------------------------------------------
        //
        //  Private Structures / Classes
        //
        //------------------------------------------------------

        #region Private Structures Classes 

        /// <summary>
        /// Dirty range entry
        /// </summary>
        private struct Entry
        {
            internal TableCell Cell;     //  reference to object (cell)
            internal int Start;     //  first dirty index
            internal int Range;     //  number of dirty indices (right after Start)
            internal int Ttl;       //  time to live counter
        }

        #endregion Private Structures Classes 
    }
}
