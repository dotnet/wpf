// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//+-----------------------------------------------------------------------
//
//
//
//  Contents:  Generic span types
//
//             [As of this creation, C# has no real generic type system]
//

using System.Windows;
using System;
using System.Collections;
using MS.Utility;
using System.Diagnostics;

namespace MS.Internal
{    
    /// <summary>
    /// VECTOR: A series of spans
    /// </summary>
    internal class SpanVector : IEnumerable
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public SpanVector(object defaultObject)
            : this(
                defaultObject,
                new FrugalStructList<Span>()
                )
        {}

        internal SpanVector(
            object      defaultObject,
            FrugalStructList<Span>   spans
            )
        {
            _defaultObject = defaultObject;
            _spans = spans;
        }


        /// <summary>
        /// Get enumerator to vector
        /// </summary>
        public IEnumerator GetEnumerator()
        {
            return new SpanEnumerator(this);
        }

        /// <summary>
        /// Add a new span to vector
        /// </summary>
        private void Add(Span span)
        {
            _spans.Add(span);
        }

        /// <summary>
        /// Delete n elements of vector
        /// </summary>
        internal virtual void Delete(int index, int count, ref SpanPosition latestPosition)
        {
            DeleteInternal(index, count);

            if (index <= latestPosition.Index)
                latestPosition = new SpanPosition();
        }

        private void DeleteInternal(int index, int count)
        {
            // Do removes highest index to lowest to minimize the number
            // of array entires copied.
            for (int i = index + count - 1; i >= index; --i)
            {
                _spans.RemoveAt(i);
            }
        }

        /// <summary>
        /// Insert n elements to vector
        /// </summary>
        private void Insert(int index, int count)
        {
            for (int c = 0; c < count; c++)
                _spans.Insert(index, new Span(null, 0));
        }

        #region Public operation

        /// <summary>
        /// Finds the span that contains the specified character position.
        /// </summary>
        /// <param name="cp">position to find</param>
        /// <param name="latestPosition">Position of the most recently accessed span (e.g., the current span
        /// of a SpanRider) for performance; FindSpan runs in O(1) time if the specified cp is in the same span 
        /// or an adjacent span.</param>
        /// <param name="spanPosition">receives the index and first cp of the span that contains the specified 
        /// position or, if the position is past the end of the vector, the index and cp just past the end of 
        /// the last span.</param>
        /// <returns>Returns true if cp is in range or false if not.</returns>
        internal bool FindSpan(int cp, SpanPosition latestPosition, out SpanPosition spanPosition)
        {
            Debug.Assert(cp >= 0);

            int spanCount = _spans.Count;
            int spanIndex, spanCP;

            if (cp == 0)
            {
                // CP zero always corresponds to span index zero
                spanIndex = 0;
                spanCP = 0;
            }
            else if (cp >= latestPosition.CP || cp * 2 < latestPosition.CP)
            {
                // One of the following is true:
                //  1.  cp is after the latest position (the most recently accessed span)
                //  2.  cp is closer to zero than to the latest position
                if (cp >= latestPosition.CP)
                {
                    // case 1: scan forward from the latest position
                    spanIndex = latestPosition.Index;
                    spanCP = latestPosition.CP;
                }
                else
                {
                    // case 2: scan forward from the start of the span vector
                    spanIndex = 0;
                    spanCP = 0;
                }

                // Scan forward until we find the Span that contains the specified CP or
                // reach the end of the SpanVector
                for (; spanIndex < spanCount; ++spanIndex)
                {
                    int spanLength = _spans[spanIndex].length;

                    if (cp < spanCP + spanLength)
                    {
                        break;
                    }

                    spanCP += spanLength;
                }
            }
            else
            {
                // The specified CP is before the latest position but closer to it than to zero;
                // therefore scan backwards from the latest position
                spanIndex = latestPosition.Index;
                spanCP = latestPosition.CP;

                while (spanCP > cp)
                {
                    Debug.Assert(spanIndex > 0);
                    spanCP -= _spans[--spanIndex].length;
                }
            }

            // Return index and cp of span in out param.
            spanPosition = new SpanPosition(spanIndex, spanCP);

            // Return true if the span is in range.
            return spanIndex != spanCount;
        }
        

        /// <summary>
        /// Set an element as a value to a character range
        /// </summary>
        /// <remarks>
        /// Implementation of span element object must implement Object.Equals to
        /// avoid runtime reflection cost on equality check of nested-type object.
        /// </remarks>
        public void SetValue(int first, int length, object element)
        {
            Set(first, length, element, SpanVector._equals, new SpanPosition());
        }

        /// <summary>
        /// Set an element as a value to a character range; takes a SpanPosition of a recently accessed
        /// span for performance and returns a known valid SpanPosition
        /// </summary>
        public SpanPosition SetValue(int first, int length, object element, SpanPosition spanPosition)
        {
            return Set(first, length, element, SpanVector._equals, spanPosition);
        }

        /// <summary>
        /// Set an element as a reference to a character range
        /// </summary>
        public void SetReference(int first, int length, object element)
        {
            Set(first, length, element, SpanVector._referenceEquals, new SpanPosition());
        }

        /// <summary>
        /// Set an element as a reference to a character range; takes a SpanPosition of a recently accessed
        /// span for performance and returns a known valid SpanPosition
        /// </summary>
        public SpanPosition SetReference(int first, int length, object element, SpanPosition spanPosition)
        {
            return Set(first, length, element, SpanVector._referenceEquals, spanPosition);
        }

        #endregion


        private SpanPosition Set(int first, int length, object element, Equals equals, SpanPosition spanPosition)
        {
            bool inRange = FindSpan(first, spanPosition, out spanPosition);

            // fs = index of first span partly or completely updated
            // fc = character index at start of fs
            int fs = spanPosition.Index;
            int fc = spanPosition.CP;

            // Find the span that contains the first affected cp
            if (!inRange)
            {
                // The first cp is past the end of the last span
                if (fc < first)
                {
                    // Create default run up to first
                    Add(new Span(_defaultObject, first - fc));
                }

                if (   Count > 0 
                    && equals(_spans[Count-1].element, element))
                {
                    // New Element matches end Element, just extend end Element
                    _spans[Count - 1].length += length;

                    // Make sure fs and fc still agree
                    if (fs == Count)
                    {
                        fc += length;
                    }
                }
                else
                {
                    Add(new Span(element, length));
                }
            }
            else
            {
                // Now find the last span affected by the update

                int ls = fs;
                int lc = fc;
                while (   ls < Count 
                       && lc + _spans[ls].length <= first + length)
                {
                    lc += _spans[ls].length;
                    ls++;
                }
                // ls = first span following update to remain unchanged in part or in whole
                // lc = character index at start of ls

                // expand update region backwards to include existing Spans of identical
                // Element type

                if (first == fc)
                {
                    // Item at [fs] is completely replaced. Check prior item

                    if (fs > 0
                        && equals(_spans[fs - 1].element, element))
                    {
                        // Expand update area over previous run of equal classification
                        fs--;
                        fc -= _spans[fs].length;
                        first = fc;
                        length += _spans[fs].length;
                    }
                }
                else
                {
                    // Item at [fs] is partially replaced. Check if it is same as update
                    if (equals(_spans[fs].element, element))
                    {
                        // Expand update area back to start of first affected equal valued run
                        length = first + length - fc;
                        first = fc;
                    }
                }

                // Expand update region forwards to include existing Spans of identical
                // Element type

                if (   ls < Count
                    && equals(_spans[ls].element, element))
                {
                    // Extend update region to end of existing split run

                    length = lc + _spans[ls].length - first;
                    lc += _spans[ls].length;
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
                        _spans[fs].length = first - fc;
                        _spans[fs + 1] = new Span(element, length);
                    }
                    else
                    {
                        // Updated item replaces [fs]
                        if (Count != fs + 1)
                        {
                            if (!Resize(fs + 1))
                                throw new OutOfMemoryException();
                        }
                        _spans[fs] = new Span(element, length);
                    }
                }
                else
                {
                    // Record partial elementtype at end, if any

                    object trailingElement = null;
                    int trailingLength = 0;

                    if (first + length > lc)
                    {
                        trailingElement = _spans[ls].element;
                        trailingLength = lc + _spans[ls].length - (first + length);
                    }

                    // Calculate change in number of Spans

                    int spanDelta = 1                          // The new span
                                    + (first > fc ? 1 : 0)      // part span at start
                                    - (ls - fs);                   // existing affected span count

                    // Note part span at end doesn't affect the calculation - the run may need
                    // updating, but it doesn't need creating.

                    if (spanDelta < 0)
                    {
                        DeleteInternal(fs + 1, -spanDelta);
                    }
                    else if (spanDelta > 0)
                    {
                        Insert(fs + 1, spanDelta);
                        // Initialize inserted Spans
                        for (int i = 0; i < spanDelta; i++)
                        {
                            _spans[fs + 1 + i] = new Span(null, 0);
                        }
                    }

                    // Assign Element values

                    // Correct Length of split span before updated range

                    if (fc < first)
                    {
                        _spans[fs].length = first - fc;
                        fs++;
                        fc = first;
                    }

                    // Record Element type for updated range

                    _spans[fs] = new Span(element, length);
                    fs++;
                    fc += length;

                    // Correct Length of split span following updated range

                    if (lc < first + length)
                    {
                        _spans[fs] = new Span(trailingElement, trailingLength);
                    }
                }
            }

            // Return a known valid span position.
            return new SpanPosition(fs, fc);
        }

        #region Properties
        /// <summary>
        /// Number of spans in vector
        /// </summary>
        public int Count
        {
            get { return _spans.Count; }
        }

        /// <summary>
        /// The default element of vector
        /// </summary>
        public object Default
        {
            get { return _defaultObject; }
        }

        /// <summary>
        /// Span accessor at nth element
        /// </summary>
        public Span this[int index]
        {
            get { return _spans[index]; }
        }
        #endregion

        #region Private members
        private bool Resize(int targetCount)
        {
            if (targetCount > Count)
            {
                for (int c = 0; c < targetCount - Count; c++)
                {
                    _spans.Add(new Span(null, 0));
                }
            }
            else if (targetCount < Count)
            {
                DeleteInternal(targetCount, Count - targetCount);
            }
            return true;
        }

        static private Equals _referenceEquals = new Equals(Object.ReferenceEquals);
        static private Equals _equals = new Equals(Object.Equals);

        FrugalStructList<Span>  _spans;
        object                  _defaultObject;
        #endregion
    }


    /// <summary>
    /// Equality check method
    /// </summary>
    internal delegate bool Equals(object first, object second);


    /// <summary>
    /// ENUMERATOR: To navigate a vector through its element
    /// </summary>
    internal sealed class SpanEnumerator : IEnumerator
    {
        internal SpanEnumerator(SpanVector spans)
        {
            _spans = spans;
            _current = -1;
        }

        /// <summary>
        /// The current span
        /// </summary>
        public object Current
        {
            get { return _spans[_current]; }
        }

        /// <summary>
        /// Move to the next span
        /// </summary>
        public bool MoveNext()
        {
            _current++;
            return _current < _spans.Count ? true : false;
        }

        /// <summary>
        /// Reset the enumerator
        /// </summary>
        public void Reset()
        {
            _current = -1;
        }

        SpanVector  _spans;
        int         _current;    // current span
    }


    /// <summary>
    /// Represents a Span's position as a pair of related values: its index in the 
    /// SpanVector its CP offset from the start of the SpanVector.
    /// </summary>
    internal struct SpanPosition
    {
        internal SpanPosition(int spanIndex, int spanCP)
        {
            _spanIndex = spanIndex;
            _spanCP = spanCP;
        }

        internal int Index
        {
            get { return _spanIndex; }
        }

        internal int CP
        {
            get { return _spanCP; }
        }

        private int _spanIndex;
        private int _spanCP;
    }


    /// <summary>
    /// RIDER: To navigate a vector through character index
    /// </summary>
    internal struct SpanRider
    {
        public SpanRider(SpanVector spans) : this(spans, new SpanPosition(), 0)
        {
        }

        public SpanRider(SpanVector spans, SpanPosition latestPosition) : this(spans, latestPosition, latestPosition.CP)
        {
        }

        public SpanRider(SpanVector spans, SpanPosition latestPosition, int cp)
        {
            _spans = spans;
            _spanPosition = new SpanPosition();
            _cp = 0;
            _cch = 0;
            At(latestPosition, cp);
        }

        /// <summary>
        /// Move rider to a given cp
        /// </summary>
        public bool At(int cp)
        {
            return At(_spanPosition, cp);
        }

        public bool At(SpanPosition latestPosition, int cp)
        {
            bool inRange = _spans.FindSpan(cp, latestPosition, out _spanPosition);
            if (inRange)
            {
                // cp is in range:
                //  - Length is the distance to the end of the span
                //  - CurrentPosition is cp
                _cch = _spans[_spanPosition.Index].length - (cp - _spanPosition.CP);
                _cp = cp;
            }
            else
            {
                // cp is out of range:
                //  - Length is the default span length
                //  - CurrentPosition is the end of the last span
                _cch = int.MaxValue;
                _cp = _spanPosition.CP;
            }

            return inRange;
        }

        #region Properties
        /// <summary>
        /// The first cp of the current span
        /// </summary>
        public int  CurrentSpanStart
        {
            get { return _spanPosition.CP; }
        }

        /// <summary>
        /// The length of current span start from the current cp
        /// </summary>
        public int  Length
        {
            get { return _cch; }
        }

        /// <summary>
        /// The current position
        /// </summary>
        public int  CurrentPosition
        {
            get { return _cp; }
        }

        /// <summary>
        /// The element of the current span
        /// </summary>
        public object CurrentElement
        {
            get { return _spanPosition.Index >= _spans.Count ? _spans.Default : _spans[_spanPosition.Index].element; }
        }

        /// <summary>
        /// Index of the span at the current position.
        /// </summary>
        public int CurrentSpanIndex
        {
            get { return _spanPosition.Index; }
        }

        /// <summary>
        /// Index and first cp of the current span.
        /// </summary>
        public SpanPosition SpanPosition
        {
            get { return _spanPosition; }
        }

        #endregion

        #region Private members
        SpanVector      _spans;         // vector of spans
        SpanPosition    _spanPosition;  // index and cp of current span
        int             _cp;            // current cp (may be greater than start of span)
        int             _cch;           // distance from current cp to end of current span
        #endregion
    }
}
