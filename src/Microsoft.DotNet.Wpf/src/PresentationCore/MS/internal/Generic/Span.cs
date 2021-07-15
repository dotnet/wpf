// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: Pairing of value and the number of positions sharing that value.
//
//


using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Diagnostics;
using MS.Utility;


namespace MS.Internal.Generic
{
    /// <summary>
    /// Pairing of value and the number of positions sharing that value.
    /// </summary>
    internal struct Span<T>
    {
        internal T      Value;
        internal int    Length;
        
        /// <summary>
        /// Construct a span of value of type 
        /// </summary>
        internal Span(T value, int  length)
        {
            Value = value;
            Length = length;
        }
    }


    /// <summary>
    /// Collection of spans
    /// </summary>
    internal struct SpanVector<T> : IEnumerable<Span<T>>
    {
        private FrugalStructList<Span<T>>   _spanList;
        private T                           _defaultValue;
        

        /// <summary>
        /// Construct a collection of spans
        /// </summary>
        internal SpanVector(T defaultValue)
            : this(defaultValue, new FrugalStructList<Span<T>>())
        {}


        private SpanVector(
            T                           defaultValue,
            FrugalStructList<Span<T>>   spanList
            )
        {
            _defaultValue = defaultValue;
            _spanList = spanList;
        }


        /// <summary>
        /// Get Generic enumerator of span vector
        /// </summary>
        public IEnumerator<Span<T>> GetEnumerator()
        {
            return new SpanEnumerator<T>(this);
        }


        /// <summary>
        /// Get enumerator of span vector
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }


        /// <summary>
        /// Add a new span to span vector
        /// </summary>
        private void Add(Span<T> span)
        {
            _spanList.Add(span);
        }


        /// <summary>
        /// Delete n elements of vector
        /// </summary>
        internal void Delete(int index, int count)
        {
            // Do removes highest index to lowest to minimize the number
            // of array entires copied.
            for (int i = index + count - 1; i >= index; --i)
            {
                _spanList.RemoveAt(i);
            }
        }


        /// <summary>
        /// Insert n elements to span vector
        /// </summary>
        private void Insert(int index, int count)
        {
            for (int i = 0; i < count; i++)
                _spanList.Insert(index, new Span<T>());
        }


        /// <summary>
        /// Set a value to a range in span vector
        /// </summary>
        internal void Set(int first, int length, T value)
        {
            // Identify first span affected by update

            int fs = 0;     // First affected span index
            int  fc = 0;     // Character position at start of first affected span

            while ( fs < Count
                &&  fc + _spanList[fs].Length <= first)
            {
                fc += _spanList[fs].Length;
                fs++;
            }

            // If the span list terminated before first, just add the new span

            if (fs >= Count)
            {
                // Ran out of Spans before reaching first

                Debug.Assert(fc <= first);

                if (fc < first)
                {
                    // Create default run up to first
                    Add(new Span<T>(_defaultValue, first-fc));
                }

                if (    Count > 0
                    &&  _spanList[Count - 1].Value.Equals(value))
                {
                    // New Element matches end Element, just extend end Element
                    Span<T> lastSpan = _spanList[Count - 1];
                    _spanList[Count - 1] = new Span<T>(lastSpan.Value, lastSpan.Length + length);
                }
                else
                {
                    Add(new Span<T>(value, length));
                }

                return;
            }

            // fs = index of first span partly or completely updated
            // fc = character index at start of fs

            // Now find the last span affected by the update

            int ls = fs;
            int  lc = fc;

            while (    ls < Count
                    && lc + _spanList[ls].Length <= first + length)
            {
                lc += _spanList[ls].Length;
                ls++;
            }

            // ls = first span following update to remain unchanged in part or in whole
            // lc = character index at start of ls

            // expand update region backwards to include existing Spans of identical
            // Element type

            if (first == fc)
            {
                // Item at [fs] is completely replaced. Check prior item

                if (    fs > 0
                    &&  _spanList[fs - 1].Value.Equals(value))
                {
                    // Expand update area over previous run of equal classification
                    fs--;
                    fc -= _spanList[fs].Length;
                    first = fc;
                    length += _spanList[fs].Length;
                }
            }
            else
            {
                // Item at [fs] is partially replaced. Check if it is same as update
                if (_spanList[fs].Value.Equals(value))
                {
                    // Expand update area back to start of first affected equal valued run
                    length = first + length - fc;
                    first = fc;
                }
            }

            // Expand update region forwards to include existing Spans of identical
            // Element type

            if (    ls < Count
                &&  _spanList[ls].Value.Equals( value))
            {
                // Extend update region to end of existing split run

                length = lc + _spanList[ls].Length - first;
                lc += _spanList[ls].Length;
                ls++;
            }

            // If no old Spans remain beyond area affected by update, handle easily:

            if (ls >= Count)
            {
                // None of the old span list extended beyond the update region

                if (fc < first)
                {
                    // Updated region leaves some of [fs]

                    if (Count != fs + 2)
                    {
                        if (!Resize(fs + 2))
                            throw new OutOfMemoryException();
                    }

                    Span<T> currentSpan = _spanList[fs];
                    _spanList[fs] = new Span<T>(currentSpan.Value, first - fc);
                    _spanList[fs + 1] = new Span<T>(value, length);
                }
                else
                {
                    // Updated item replaces [fs]
                    if (Count != fs + 1)
                    {
                        if (!Resize(fs + 1))
                            throw new OutOfMemoryException();
                    }

                    _spanList[fs] = new Span<T>(value, length);
                }

                return;  // DONE
            }

            // Record partial elementtype at end, if any

            T trailingValue = (new Span<T>()).Value;
            int trailingLength = 0;

            if (first + length > lc)
            {
                trailingValue = _spanList[ls].Value;
                trailingLength  = lc + _spanList[ls].Length - (first + length);
            }

                // Calculate change in number of Spans

            int spanDelta =    1                          // The new span
                            +  (first  > fc ? 1 : 0)      // part span at start
                            -  (ls - fs);                 // existing affected span count

            // Note part span at end doesn't affect the calculation - the run may need
            // updating, but it doesn't need creating.

            if (spanDelta < 0)
            {
                Delete(fs + 1, -spanDelta);
            }
            else if (spanDelta > 0)
            {
                Insert(fs + 1, spanDelta);

                // Initialize inserted Spans
                for (int i = 0; i < spanDelta; i++)
                {
                    _spanList[fs + 1 + i] = new Span<T>();
                }
            }

            // Assign Element values

            // Correct Length of split span before updated range

            if (fc < first)
            {
                Span<T> currentSpan = _spanList[fs];
                _spanList[fs] = new Span<T>(currentSpan.Value, first - fc);
                fs++;
            }

            // Record Element type for updated range

            _spanList[fs] = new Span<T>(value, length);
            fs++;

            // Correct Length of split span following updated range

            if (lc < first + length)
            {
                _spanList[fs] = new Span<T>(trailingValue, trailingLength);
            }

            // Phew, all done ....

            return;
        }


        /// <summary>
        /// Number of spans in span vector
        /// </summary>
        internal int Count
        {
            get { return _spanList.Count; }
        }


        /// <summary>
        /// The default value of span vector
        /// </summary>
        internal T DefaultValue
        {
            get { return _defaultValue; }
        }


        /// <summary>
        /// Indexer of span vector
        /// </summary>
        internal Span<T> this[int index]
        {
            get { return _spanList[index]; }
        }


        private bool Resize(int targetCount)
        {
            if (targetCount > Count)
            {
                for (int c = 0; c < targetCount - Count; c++)
                {
                    _spanList.Add(new Span<T>());
                }
            }
            else if (targetCount < Count)
            {
                Delete(targetCount, Count - targetCount);
            }

            return true;
        }



        /// <summary>
        /// Enumerator of span vector to facilitate iterating of spans
        /// </summary>
        private struct SpanEnumerator<U> : IEnumerator<Span<U>>
        {
            private SpanVector<U>   _vector;
            private int             _current;


            internal SpanEnumerator(SpanVector<U> vector)
            {
                _vector = vector;
                _current = -1;
            }


            void IDisposable.Dispose()
            { }


            /// <summary>
            /// The current span
            /// </summary>
            public Span<U> Current
            {
                get { return _vector[_current]; }
            }


            /// <summary>
            /// The current span
            /// </summary>
            object IEnumerator.Current
            {
                get { return this.Current; }
            }


            /// <summary>
            /// Move to the next span
            /// </summary>
            public bool MoveNext()
            {
                _current++;
                return _current < _vector.Count;
            }

            /// <summary>
            /// Reset the enumerator
            /// </summary>
            public void Reset()
            {
                _current = -1;
            }
        }
    }


    /// <summary>
    /// Span rider facilitates random access of value at any position in the span vector
    /// </summary>
    internal struct SpanRider<T>
    {
        private const int MaxCch = int.MaxValue;

        private SpanVector<T>   _vector;
        private Span<T>         _defaultSpan;
        private int             _current;    // current span
        private int             _cp;         // current cp
        private int             _dcp;        // dcp from start to the start of current span
        private int             _cch;        // length of the current span


        internal SpanRider(SpanVector<T> vector)
        {
            _defaultSpan = new Span<T>(vector.DefaultValue, MaxCch);
            _vector = vector;
            _current = 0;
            _cp = 0;
            _dcp = 0;
            _cch = 0;
            At(0);
        }


        /// <summary>
        /// Position the rider at the specfied position
        /// </summary>
        /// <param name="cp">position to move to</param>
        internal bool At(int cp)
        {
#if DEBUG
            {
                // Check that current position details are valid
                int dcp = 0;
                int i = 0;

                // advance to current value start
                while (dcp < _dcp && i < _vector.Count)
                {
                    dcp += _vector[i].Length;
                    i++;
                }

                Debug.Assert(
                        i <= _vector.Count      // current value is within valid range
                    &&  i == _current           // current value is valid
                    &&  dcp == _dcp             // current value start is valid
                    &&  (   i == _vector.Count
                        ||  _cp <= dcp + _vector[i].Length),    // current cp is within range of current value
                    "Span vector is corrupted!"
                    );
            }
#endif

            if (cp < _dcp)
            {
                // Need to start from 0 again
                _cp = _dcp = _current = _cch = 0;
            }

            // Advance to value containing cp

            Span<T> span = new Span<T>();

            while(  _current < _vector.Count
                &&  _dcp + (span = _vector[_current]).Length <= cp)
            {
                _dcp += span.Length;
                _current++;
            }

            if (_current < _vector.Count)
            {
                _cch = _vector[_current].Length - cp + _dcp;
                _cp = cp;
                return true;
            }
            else
            {
                _cch = _defaultSpan.Length;
                _cp = Math.Min(cp, _dcp);
                return false;
            }
        }


        /// <summary>
        /// The first position of the current span
        /// </summary>
        internal int CurrentSpanStart
        {
            get { return _dcp; }
        }


        /// <summary>
        /// The remaining length of the current span start from the current position
        /// </summary>
        internal int Length
        {
            get { return _cch; }
        }


        /// <summary>
        /// The current position
        /// </summary>
        internal int CurrentPosition
        {
            get { return _cp; }
        }


        /// <summary>
        /// The value of the current span
        /// </summary>
        internal T CurrentValue
        {
            get { return _current >= _vector.Count ? _defaultSpan.Value : _vector[_current].Value; }
        }
    }
}
