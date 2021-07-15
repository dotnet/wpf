// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: HighlightLayer for annotations. It handles highlights
// and StickyNote anchors as well.
//

using System;
using MS.Internal;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Documents;
using MS.Internal.Documents;
using System.Windows.Media;
using MS.Internal.Text;
using System.Windows.Shapes;
using MS.Internal.Annotations.Anchoring;
using System.Windows.Controls;

namespace MS.Internal.Annotations.Component
{
    // Highlight rendering for the Annotation highlight and sticky note anchor.
    internal class AnnotationHighlightLayer : HighlightLayer
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Creates a new instance of the highlight layer
        /// </summary>
        internal AnnotationHighlightLayer()
        {
            _segments = new List<HighlightSegment>();
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Adds a new text range to the layer
        /// </summary>
        /// <remarks>
        /// If the range does not overlap any of the existing segments it is added on the appropriate position
        /// in the _segments array - the _segments array contains non overlapping HighlightSegments sorted by ITextPointers.
        /// If the range overlaps some of the existing segments its IHighlightRange is added to the list of owners
        /// for those segments. If the input range overlaps partially some of the existing segments they will be split into
        /// new segments by the corresponding range end points. For the overlapping parts the IHighlightRange of the new
        /// range will be added. If the new range overlaps parts of existing segments and some nonhighlighted areas, new
        /// HighlightSegments will be generated for the nonhighlighted areas. Example:
        /// 1. text "1234567" has 2 HighlightSegments covereing "234" and "67"
        /// 2. Add a range that covers "3456"
        /// 3.The result will be 5 HighlightSegments - "2", "34", "5", "6" and "7". "2" and "7" will keep their original owners array.
        ///   "5" will have only one owner - the new IHighlightRange, "34" and "6" will have their previous owners + the new one.
        /// </remarks>
        /// <param name="highlightRange">the highlight range owner</param>
        internal void AddRange(IHighlightRange highlightRange)
        {
            Invariant.Assert(highlightRange != null, "the owner is null");
            ITextPointer start = highlightRange.Range.Start;
            ITextPointer end = highlightRange.Range.End;
            //check input data
            Debug.Assert(start != null, "start pointer is null");
            Debug.Assert(end != null, "end pointer is null");
            Debug.Assert(start.CompareTo(end) <= 0, "end pointer before start");

            if (start.CompareTo(end) == 0)
            {
                //it is a 0 length highlight - do not render it
                return;
            }

            if (_segments.Count == 0)
            {
                //set container type
                object textContainer = start.TextContainer;
                IsFixedContainer = textContainer is FixedTextContainer || textContainer is DocumentSequenceTextContainer;
            }

            ITextPointer invalidateStart;
            ITextPointer invalidateEnd;

            ProcessOverlapingSegments(highlightRange, out invalidateStart, out invalidateEnd);

            //fire event - do it only for fixed to avoid disposing of the page in flow. Needs to be changed in V2.
            if ((Changed != null) && IsFixedContainer)
            {
                Changed(this, new AnnotationHighlightChangedEventArgs(invalidateStart, invalidateEnd));
            }
        }

        /// <summary>
        /// RemoveRange from the highlight layer. The corresponding IHighlightRange object will be removed
        /// from all HighlightSegments that belong to this range. All ranges with no more owners will also be removed.
        /// The visual properties of ranges that have more owners might change.
        /// </summary>
        /// <param name="highlightRange">The highlight range owner</param>
        /// <returns>true if the range is successfuly removed</returns>
        internal void RemoveRange(IHighlightRange highlightRange)
        {
            Debug.Assert(highlightRange != null, "null range data");

            //if range is 0 length do nothing
            if (highlightRange.Range.Start.CompareTo(highlightRange.Range.End) == 0)
                return;

            int startSeg;
            int endSeg;
            GetSpannedSegments(highlightRange.Range.Start, highlightRange.Range.End, out startSeg, out endSeg);

            //get invalidate start and end point
            ITextPointer invalidateStart = _segments[startSeg].Segment.Start;
            ITextPointer invalidateEnd = _segments[endSeg].Segment.End;
            for (int i = startSeg; i <= endSeg;)
            {
                HighlightSegment highlightSegment = _segments[i];
                int count = highlightSegment.RemoveOwner(highlightRange);
                if (count == 0)
                {
                    _segments.Remove(highlightSegment);
                    endSeg--;
                }
                else
                {
                    i++;
                }
            }

            //TBD:Should do something against fragmentation

            //fire event - do it only for fixed to avoid disposing of the page in flow. Needs to be changed in V2.
            if ((Changed != null) && IsFixedContainer)
            {
                Changed(this, new AnnotationHighlightChangedEventArgs(invalidateStart, invalidateEnd));
            }
        }

        /// <summary>
        /// Notifies segments and listeners that a IHighlightRange colors have been modified
        /// </summary>
        /// <param name="highlightRange">the highlight range to be modified</param>
        /// <returns>true if successfuly modified</returns>
        internal void ModifiedRange(IHighlightRange highlightRange)
        {
            Invariant.Assert(highlightRange != null, "null range data");

            //if range is 0 length do nothing
            if (highlightRange.Range.Start.CompareTo(highlightRange.Range.End) == 0)
                return;

            int startSeg;
            int endSeg;
            GetSpannedSegments(highlightRange.Range.Start, highlightRange.Range.End, out startSeg, out endSeg);

            //update colors
            for (int seg = startSeg; seg < endSeg; seg++)
            {
                //the owners have not changed so this will only update the colors
                _segments[seg].UpdateOwners();
            }

            //get invalidate start and end point
            ITextPointer invalidateStart = _segments[startSeg].Segment.Start;
            ITextPointer invalidateEnd = _segments[endSeg].Segment.End;

            //fire event - do it only for fixed to avoid disposing of the page in flow. Needs to be changed in V2.
            if ((Changed != null) && IsFixedContainer)
            {
                Changed(this, new AnnotationHighlightChangedEventArgs(invalidateStart, invalidateEnd));
            }
        }

        /// <summary>
        /// Activate/Deactivate highlight range 
        /// </summary>
        /// <param name="highlightRange">the text range to be modified</param>
        /// <param name="activate">true - activate, false - deactivate</param>
        internal void ActivateRange(IHighlightRange highlightRange, bool activate)
        {
            Invariant.Assert(highlightRange != null, "null range data");

            //if range is 0 length do nothing
            if (highlightRange.Range.Start.CompareTo(highlightRange.Range.End) == 0)
                return;

            int startSeg;
            int endSeg;
            GetSpannedSegments(highlightRange.Range.Start, highlightRange.Range.End, out startSeg, out endSeg);

            //get invalidate start and end point
            ITextPointer invalidateStart = _segments[startSeg].Segment.Start;
            ITextPointer invalidateEnd = _segments[endSeg].Segment.End;

            //set them as active
            for (int i = startSeg; i <= endSeg; i++)
            {
                if (activate)
                    _segments[i].AddActiveOwner(highlightRange);
                else
                    _segments[i].RemoveActiveOwner(highlightRange);
            }

            //fire event - do it only for fixed to avoid disposing of the page in flow. Needs to be changed in V2.
            if ((Changed != null) && IsFixedContainer)
            {
                Changed(this, new AnnotationHighlightChangedEventArgs(invalidateStart, invalidateEnd));
            }
        }

        /// <summary>
        /// Returns the value of a property stored on scoping highlight, if any.
        /// If no property value is set, returns null.
        /// </summary>
        /// <remarks>
        /// We scan all the segments in a loop end for each segment make two checks:
        /// 1. Is the textPosition before the beginning of the segment, or at the begining with
        /// GicalDirection.Backward. If this is true our point is before the current segment.
        /// We know that if it belongs to any of the previous segments the loop will stop, so that
        /// means the point is outside any segments - break the loop.
        /// 2. If the textPosition is not before the current segment, check if it is on the current segment
        /// or at the end with LogicalDirection.Backward. If this is true the point belongs to the
        /// current segment - save it and break the loop.
        /// </remarks>
        /// <param name="textPosition">position to check for</param>
        /// <param name="direction">logical direction</param>
        /// <returns></returns>
        internal override object GetHighlightValue(StaticTextPointer textPosition, LogicalDirection direction)
        {
            object value = DependencyProperty.UnsetValue;
            HighlightSegment highlightSegment = null;

            for (int i = 0; i < _segments.Count; i++)
            {
                highlightSegment = _segments[i];
                if ((highlightSegment.Segment.Start.CompareTo(textPosition) > 0) ||
                    ((highlightSegment.Segment.Start.CompareTo(textPosition) == 0) && (direction == LogicalDirection.Backward)))
                {
                    // the point is outside highlights
                    break;
                }

                if ((highlightSegment.Segment.End.CompareTo(textPosition) > 0) ||
                    ((highlightSegment.Segment.End.CompareTo(textPosition) == 0) && (direction == LogicalDirection.Backward)))
                {
                    value = highlightSegment;
                    break;
                }
            }

            return value;
        }

        // Returns true if the indicated content has scoping highlights.
        internal override bool IsContentHighlighted(StaticTextPointer staticTextPosition, LogicalDirection direction)
        {
            return GetHighlightValue(staticTextPosition, direction) != DependencyProperty.UnsetValue;
        }

        // Returns the position of the next highlight start or end in an
        // indicated direction, or null if there is no such position.
        internal override StaticTextPointer GetNextChangePosition(StaticTextPointer textPosition, LogicalDirection direction)
        {
            ITextPointer dynamicPosition;

            if (direction == LogicalDirection.Forward)
            {
                dynamicPosition = GetNextForwardPosition(textPosition);
            }
            else
            {
                dynamicPosition = GetNextBackwardPosition(textPosition);
            }

            return dynamicPosition == null ? StaticTextPointer.Null : dynamicPosition.CreateStaticPointer();
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        /// <summary>
        /// Type identifying this layer for Highlights.GetHighlightValue calls.
        /// </summary>
        internal override Type OwnerType
        {
            get
            {
                return typeof(HighlightComponent);
            }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Internal Events
        //
        //------------------------------------------------------

        #region Internal Events

        // Event raised when a highlight range is inserted, removed, moved, or
        // has a local property value change.
        internal override event HighlightChangedEventHandler Changed;

        #endregion Internal Events

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// Checks if the input range overlaps existing segments and splits them if needed
        /// </summary>
        /// <param name="highlightRange">range data</param>
        /// <param name="invalidateStart">start pointer of the invalid area</param>
        /// <param name="invalidateEnd">end pointer of the invalid area</param>
        /// <remarks>This method requires ordered nonoverlaped input segments. Otherwise it will assert.</remarks>
        private void ProcessOverlapingSegments(IHighlightRange highlightRange, out ITextPointer invalidateStart, out ITextPointer invalidateEnd)
        {
            Debug.Assert(highlightRange != null, " null highlight range");
            ReadOnlyCollection<TextSegment> rangeSegments = highlightRange.Range.TextSegments;
            Debug.Assert((rangeSegments != null) && (rangeSegments.Count > 0), "invalid rangeSegments");


            invalidateStart = null;
            invalidateEnd = null;
            int ind = 0;
            IEnumerator<TextSegment> rangeEnumerator = rangeSegments.GetEnumerator();
            TextSegment rangeSegment = rangeEnumerator.MoveNext() ? rangeEnumerator.Current : TextSegment.Null;
            while ((ind < _segments.Count) && (!rangeSegment.IsNull))
            {
                HighlightSegment highlightSegment = _segments[ind];
                Debug.Assert(highlightSegment != null, "null highlight segment");

                if (highlightSegment.Segment.Start.CompareTo(rangeSegment.Start) <= 0)
                {
                    if (highlightSegment.Segment.End.CompareTo(rangeSegment.Start) > 0)
                    {
                        //split highlightSegment
                        //the split method is smart enough to take care of edge cases - point on start/end of the
                        //segment, points outside the segment etc
                        IList<HighlightSegment> res = highlightSegment.Split(rangeSegment.Start, rangeSegment.End, highlightRange);

                        //if the result does not contain the original segment we need to clear the owners
                        if (!res.Contains(highlightSegment))
                            highlightSegment.ClearOwners();

                        _segments.Remove(highlightSegment);
                        _segments.InsertRange(ind, res);
                        ind = ind + res.Count - 1;

                        //check if we need to move to next range segment
                        if (rangeSegment.End.CompareTo(highlightSegment.Segment.End) <= 0)
                        {
                            //get next one
                            bool next = rangeEnumerator.MoveNext();
                            Debug.Assert(rangeEnumerator.Current.IsNull || !next ||
                                         (rangeSegment.End.CompareTo(rangeEnumerator.Current.Start) <= 0),
                                         "overlapped range segments");
                            rangeSegment = next ? rangeEnumerator.Current : TextSegment.Null;
                        }
                        else
                        {
                            //get the piece that is left
                            rangeSegment = new TextSegment(highlightSegment.Segment.End, rangeSegment.End);
                        }

                        //set invalidateStart if needed
                        if (invalidateStart == null)
                            invalidateStart = highlightSegment.Segment.Start;
                    }
                    else
                    {
                        //move to next highlightsegment
                        ind++;
                    }
                }
                else
                {
                    //set invalidateStart if needed
                    if (invalidateStart == null)
                        invalidateStart = rangeSegment.Start;

                    if (rangeSegment.End.CompareTo(highlightSegment.Segment.Start) > 0)
                    {
                        //add the piece before the highlight segment
                        HighlightSegment temp = new HighlightSegment(rangeSegment.Start, highlightSegment.Segment.Start, highlightRange);
                        _segments.Insert(ind++, temp);

                        //now our current segment is the rest of the range segment
                        rangeSegment = new TextSegment(highlightSegment.Segment.Start, rangeSegment.End);
                    }
                    else
                    {
                        //just insert this range segment - it does not cover any highlight segnments. Increment ind
                        //so it points to the same highlight segment
                        _segments.Insert(ind++, new HighlightSegment(rangeSegment.Start, rangeSegment.End, highlightRange));
                        //get next range segment
                        rangeSegment = rangeEnumerator.MoveNext() ? rangeEnumerator.Current : TextSegment.Null;
                    }
                }
            }

            //
            if (!rangeSegment.IsNull)
            {
                if (invalidateStart == null)
                    invalidateStart = rangeSegment.Start;
                _segments.Insert(ind++, new HighlightSegment(rangeSegment.Start, rangeSegment.End, highlightRange));
            }

            //check if there are more rangeSegments
            while (rangeEnumerator.MoveNext())
            {
                _segments.Insert(ind++, new HighlightSegment(rangeEnumerator.Current.Start, rangeEnumerator.Current.End, highlightRange));
            }

            //set invalidateEnd
            if (invalidateStart != null)
            {
                if (ind == _segments.Count) ind--;
                invalidateEnd = _segments[ind].Segment.End;
            }
        }

        /// <summary>
        /// Gets next change position in the forward direction
        /// </summary>
        /// <param name="pos"> start position</param>
        /// <returns>next position if any</returns>
        private ITextPointer GetNextForwardPosition(StaticTextPointer pos)
        {
            for (int i = 0; i < _segments.Count; i++)
            {
                HighlightSegment highlightSegment = _segments[i];
                if (pos.CompareTo(highlightSegment.Segment.Start) >= 0)
                {
                    if (pos.CompareTo(highlightSegment.Segment.End) < 0)
                        return highlightSegment.Segment.End;
                }
                else
                {
                    return highlightSegment.Segment.Start;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets next change position in the backward direction
        /// </summary>
        /// <param name="pos"> start position</param>
        /// <returns>nex position if any</returns>
        private ITextPointer GetNextBackwardPosition(StaticTextPointer pos)
        {
            for (int i = _segments.Count - 1; i >= 0; i--)
            {
                HighlightSegment highlightSegment = _segments[i];
                if (pos.CompareTo(highlightSegment.Segment.End) <= 0)
                {
                    if (pos.CompareTo(highlightSegment.Segment.Start) > 0)
                        return highlightSegment.Segment.Start;
                }
                else
                {
                    return highlightSegment.Segment.End;
                }
            }

            return null;
        }

        void GetSpannedSegments(ITextPointer start, ITextPointer end, out int startSeg, out int endSeg)
        {
            startSeg = -1;
            endSeg = -1;
            //add it to Highlight segments
            for (int i = 0; i < _segments.Count; i++)
            {
                HighlightSegment highlightSegment = _segments[i];
                if (highlightSegment.Segment.Start.CompareTo(start) == 0)
                    startSeg = i;
                if (highlightSegment.Segment.End.CompareTo(end) == 0)
                {
                    endSeg = i;
                    break;
                }
            }

            if ((startSeg < 0) || (endSeg < 0) || (startSeg > endSeg))
                Debug.Assert(false, "Mismatched segment data");
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------

        #region Private Properties

        private bool IsFixedContainer
        {
            get
            {
                return _isFixedContainer;
            }

            set
            {
                _isFixedContainer = value;
            }
        }

        #endregion Private Properties

        //------------------------------------------------------
        //
        //  Private Types
        //
        //------------------------------------------------------

        #region Private Types

        // Argument for the Changed event, encapsulates a highlight change.
        private class AnnotationHighlightChangedEventArgs : HighlightChangedEventArgs
        {
            // Constructor.
            internal AnnotationHighlightChangedEventArgs(ITextPointer start, ITextPointer end)
            {
                TextSegment[] rangeArray = new TextSegment[] { new TextSegment(start, end) };

                _ranges = new ReadOnlyCollection<TextSegment>(rangeArray);
            }

            // Collection of changed content ranges.
            internal override IList Ranges
            {
                get
                {
                    return _ranges;
                }
            }

            // Type identifying the owner of the changed layer.
            internal override Type OwnerType
            {
                get
                {
                    return typeof(HighlightComponent);
                }
            }

            // Collection of changed content ranges.
            private readonly ReadOnlyCollection<TextSegment> _ranges;
        }

        /// <summary>
        /// Represent one segment of the highlight that belongs to a particular set of owners.
        /// In case of overlaping highlights one HighlightSegment will be generated for each part
        /// that hase same set of owners.
        /// </summary>
        internal sealed class HighlightSegment : Shape
        {
            //------------------------------------------------------
            //
            //  Constructors
            //
            //------------------------------------------------------

            #region Constructors

            /// <summary>
            /// Creates a new HighlightSegment with one owner
            /// </summary>
            /// <param name="start">start segment position</param>
            /// <param name="end">end segment position</param>
            /// <param name="owner">Owners data. Used to find the right place of this owner in the list</param>
            internal HighlightSegment(ITextPointer start, ITextPointer end, IHighlightRange owner)
                : base()
            {
                List<IHighlightRange> list = new List<IHighlightRange>(1);
                list.Add(owner);
                Init(start, end, list);
                _owners = list;
                UpdateOwners();
            }

            /// <summary>
            /// Creates a new HighlightSegment with a list of owners
            /// </summary>
            /// <param name="start">start segment position</param>
            /// <param name="end">end segment position</param>
            /// <param name="owners">owners list</param>
            internal HighlightSegment(ITextPointer start, ITextPointer end, IList<IHighlightRange> owners)
            {
                Init(start, end, owners);
                //make a copy of the owners
                _owners = new List<IHighlightRange>(owners.Count);
                _owners.AddRange(owners);
                UpdateOwners();
            }

            /// <summary>
            /// Creates a new HighlightSegment with a list of owners
            /// </summary>
            /// <param name="start">start segment position</param>
            /// <param name="end">end segment position</param>
            /// <param name="owners">owners list</param>
            private void Init(ITextPointer start, ITextPointer end, IList<IHighlightRange> owners)
            {
                Debug.Assert(start != null, "start pointer is null");
                Debug.Assert(end != null, "end pointer is null");
                Debug.Assert(owners != null, "null owners list");
                Debug.Assert(owners.Count > 0, "empty owners list");
                for (int i = 0; i < owners.Count; i++)
                    Debug.Assert(owners[i] != null, "null owner");

                _segment = new TextSegment(start, end);
                IsHitTestVisible = false;
                object textContainer = start.TextContainer;
                _isFixedContainer = textContainer is FixedTextContainer || textContainer is DocumentSequenceTextContainer;

                //check for tables, figures and floaters and extract the content
                GetContent();
            }

            #endregion Constructors

            //------------------------------------------------------
            //
            //  Internal methods
            //
            //------------------------------------------------------

            #region Internal methods
            /// <summary>
            /// Adds one owner to the _owners list
            /// </summary>
            /// <param name="owner">owner data</param>
            /// <returns></returns>
            internal void AddOwner(IHighlightRange owner)
            {
                //Find the right place in the list according to TimeStamp and the priority
                //Note: - currently we care only about priority
                for (int i = 0; i < _owners.Count; i++)
                {
                    if (_owners[i].Priority < owner.Priority)
                    {
                        _owners.Insert(i, owner);
                        UpdateOwners();
                        return;
                    }
                }

                //every body has higher priority - add this at the end
                _owners.Add(owner);
                UpdateOwners();
            }

            /// <summary>
            /// Removes one owner from the _owners list
            /// </summary>
            /// <param name="owner">owner data</param>
            /// <returns>the number of owners left for this HighlightSegment</returns>
            internal int RemoveOwner(IHighlightRange owner)
            {
                if (_owners.Contains(owner))
                {
                    if (_activeOwners.Contains(owner))
                        _activeOwners.Remove(owner);
                    _owners.Remove(owner);
                    UpdateOwners();
                }

                return _owners.Count;
            }

            /// <summary>
            /// Adds an owner to_activeOwners list
            /// </summary>
            /// <param name="owner">owner data</param>
            /// <returns>the number of owners left for this HighlightSegment</returns>
            internal void AddActiveOwner(IHighlightRange owner)
            {
                //this must be a valid owner
                if (_owners.Contains(owner))
                {
                    _activeOwners.Add(owner);
                    UpdateOwners();
                }
            }

            /// <summary>
            /// Adds owners range to_activeOwners list
            /// </summary>
            /// <param name="owners">owner list</param>
            /// <returns>the number of owners left for this HighlightSegment</returns>
            private void AddActiveOwners(List<IHighlightRange> owners)
            {
                _activeOwners.AddRange(owners);
                UpdateOwners();
            }

            /// <summary>
            /// Removes one owner from the _activeOwners list
            /// </summary>
            /// <param name="owner">owner data</param>
            /// <returns>the number of owners left for this HighlightSegment</returns>
            internal void RemoveActiveOwner(IHighlightRange owner)
            {
                if (_activeOwners.Contains(owner))
                {
                    _activeOwners.Remove(owner);
                    UpdateOwners();
                }
            }

            /// <summary>
            /// Clear all segment owners - most probably the segment will be removed
            /// </summary>
            internal void ClearOwners()
            {
                _owners.Clear();
                _activeOwners.Clear();
                UpdateOwners();
            }

            /// <summary>
            /// Splits this HighlightSegemnt into two highlights
            /// </summary>
            /// <param name="ps">splitting position</param>
            /// <param name="side">On which side of the splitting point to add new owner</param>
            /// <returns>A list of resulting highlights. They have new TextSegments and the same set of
            /// owners as this</returns>
            internal IList<HighlightSegment> Split(ITextPointer ps, LogicalDirection side)
            {
                IList<HighlightSegment> res = null;
                if ((ps.CompareTo(_segment.Start) == 0) || (ps.CompareTo(_segment.End) == 0))
                {
                    if (((ps.CompareTo(_segment.Start) == 0) && (side == LogicalDirection.Forward)) ||
                         ((ps.CompareTo(_segment.End) == 0) && (side == LogicalDirection.Backward)))
                    {
                        res = new List<HighlightSegment>(1);
                        res.Add(this);
                    }
                }
                else if (_segment.Contains(ps))
                {
                    res = new List<HighlightSegment>(2);
                    res.Add(new HighlightSegment(_segment.Start, ps, _owners));
                    res.Add(new HighlightSegment(ps, _segment.End, _owners));
                    res[0].AddActiveOwners(_activeOwners);
                    res[1].AddActiveOwners(_activeOwners);
                }
                return res;
            }

            /// <summary>
            /// Splits HighlightSegment in two positions
            /// </summary>
            /// <param name="ps1">first splitting position</param>
            /// <param name="ps2">second splitting position</param>
            /// <param name="newOwner">Guid of a new owner to be added in the middle. May be null</param>
            /// <returns>A list of resulting HighlightSegments. They have same list of owners as this</returns>
            internal IList<HighlightSegment> Split(ITextPointer ps1, ITextPointer ps2, IHighlightRange newOwner)
            {
                Debug.Assert((ps1 != null) && (ps2 != null) && (ps1.CompareTo(ps2) <= 0), "invalid splitting points");

                IList<HighlightSegment> res = new List<HighlightSegment>();

                if (ps1.CompareTo(ps2) == 0)
                {
                    //special processing for equal splitting points
                    if ((_segment.Start.CompareTo(ps1) > 0) || (_segment.End.CompareTo(ps1) < 0))
                        return res;

                    if (_segment.Start.CompareTo(ps1) < 0)
                    {
                        res.Add(new HighlightSegment(_segment.Start, ps1, _owners));
                    }

                    //add 0-length segment
                    res.Add(new HighlightSegment(ps1, ps1, _owners));

                    if (_segment.End.CompareTo(ps1) > 0)
                    {
                        res.Add(new HighlightSegment(ps1, _segment.End, _owners));
                    }

                    //add active owners as well
                    foreach (HighlightSegment seg in res)
                    {
                        seg.AddActiveOwners(_activeOwners);
                    }
                }
                else if (_segment.Contains(ps1))
                {
                    IList<HighlightSegment> r1 = Split(ps1, LogicalDirection.Forward);
                    for (int i = 0; i < r1.Count; i++)
                    {
                        if (r1[i].Segment.Contains(ps2))
                        {
                            IList<HighlightSegment> r2 = r1[i].Split(ps2, LogicalDirection.Backward);
                            for (int j = 0; j < r2.Count; j++)
                                res.Add(r2[j]);

                            //check if r1[i] needs to be discarded (it can be included in the split result
                            // so we should not discard it in that case)
                            if (!r2.Contains(r1[i]))
                                r1[i].Discard();
                        }
                        else
                        {
                            res.Add(r1[i]);
                        }
                    }
                }
                else
                {
                    res = Split(ps2, LogicalDirection.Backward);
                }

                if ((res != null) && (res.Count > 0) && (newOwner != null))
                {
                    //add owner
                    if (res.Count == 3)
                    {
                        //if we have 3 segments the new owner will go to the middle one
                        res[1].AddOwner(newOwner);
                    }
                    else if ((res[0].Segment.Start.CompareTo(ps1) == 0) ||
                             (res[0].Segment.End.CompareTo(ps2) == 0))
                    {
                        //if one of the splitting points is on the corresponding end of the first
                        //segment it will have the new owner
                        res[0].AddOwner(newOwner);
                    }
                    else
                    {
                        //if we have 1 segment we should go through the else above, so they must be 2
                        Debug.Assert(res.Count == 2, "unexpected resulting segment count after split");
                        res[1].AddOwner(newOwner);
                    }
                }

                return res;
            }

            internal void UpdateOwners()
            {
                if (_cachedTopOwner != TopOwner)
                {
                    //remove it from the old owner children
                    if (_cachedTopOwner != null)
                        _cachedTopOwner.RemoveChild(this);
                    _cachedTopOwner = TopOwner;

                    //add it to the new owner children
                    if (_cachedTopOwner != null)
                        _cachedTopOwner.AddChild(this);
                }
                Fill = OwnerColor;
            }

            /// <summary>
            /// this is called when this HighlightSegment will be discarded
            /// It has to remove itself from the TopOwner's children and empty the
            /// owners lists
            /// </summary>
            internal void Discard()
            {
                if (TopOwner != null)
                    TopOwner.RemoveChild(this);
                _activeOwners.Clear();
                _owners.Clear();
            }

            #endregion Internal methods

            //------------------------------------------------------
            //
            //  Private methods
            //
            //------------------------------------------------------

            #region Private methods

            /// <summary>
            /// Calculates geometry for ine TextSegment
            /// </summary>
            /// <param name="geometry">GeometryGroup to add the geometry</param>
            /// <param name="segment">TextSegment</param>
            /// <param name="parentView">TextView to which geometry has to be transformed</param>
            private void GetSegmentGeometry(GeometryGroup geometry, TextSegment segment, ITextView parentView)
            {
                List<ITextView> textViews = TextSelectionHelper.GetDocumentPageTextViews(segment);
                Debug.Assert(textViews != null, "geometry text view not found");

                foreach (ITextView view in textViews)
                {
                    Geometry viewGeometry = GetPageGeometry(segment, view, parentView);
                    if (viewGeometry != null)
                        geometry.Children.Add(viewGeometry);
                }
            }

            /// <summary>
            /// Get a geometry for a particular page and transforms it to the parent page
            /// </summary>
            /// <param name="segment">the TextSegment for which geometry we are looking</param>
            /// <param name="view">the page view</param>
            /// <param name="parentView">the parent page view</param>
            /// <returns></returns>
            private Geometry GetPageGeometry(TextSegment segment, ITextView view, ITextView parentView)
            {
                Debug.Assert((view != null) && (parentView != null), "null text view");
                //in the initial layout update the TextViews might be invalid. This is OK
                //since there will be a second pass
                if (!view.IsValid || !parentView.IsValid)
                    return null;

                //Debug.Assert((view.RenderScope != null) && (parentView.RenderScope != null), "null text view render scope");
                if ((view.RenderScope == null) || (parentView.RenderScope == null))
                    return null;

                Geometry pageGeometry = null;
                pageGeometry = view.GetTightBoundingGeometryFromTextPositions(segment.Start, segment.End);

                if (pageGeometry != null)
                {
                    if (parentView != null)
                    {
                        Transform additionalTransform = (Transform)view.RenderScope.TransformToVisual(parentView.RenderScope);
                        if (pageGeometry.Transform != null)
                        {
                            //we need to create geometry group in this case
                            TransformGroup group = new TransformGroup();
                            group.Children.Add(pageGeometry.Transform);
                            group.Children.Add(additionalTransform);
                            pageGeometry.Transform = group;
                        }
                        else
                        {
                            //now set the transformation
                            pageGeometry.Transform = additionalTransform;
                        }
                    }
                }

                return pageGeometry;
            }

            /// <summary>
            /// Checks the TextSegment for tables, figures and floaters and gets the content if any
            /// </summary>
            private void GetContent()
            {
                Debug.Assert(!_segment.IsNull, "null TextSegment");
                _contentSegments.Clear();

                ITextPointer cursor = _segment.Start.CreatePointer();
                ITextPointer segmentStart = null;

                while (cursor.CompareTo(_segment.End) < 0)
                {
                    TextPointerContext nextContext = cursor.GetPointerContext(LogicalDirection.Forward);
                    if (nextContext == TextPointerContext.ElementStart)
                    {
                        Type elementType = cursor.GetElementType(LogicalDirection.Forward);
                        if (typeof(Run).IsAssignableFrom(elementType) ||
                            typeof(BlockUIContainer).IsAssignableFrom(elementType))
                        {
                            // Open new segment if it was not opened already
                            OpenSegment(ref segmentStart, cursor);
                        }
                        else if (typeof(Table).IsAssignableFrom(elementType) ||
                                 typeof(Floater).IsAssignableFrom(elementType) ||
                                 typeof(Figure).IsAssignableFrom(elementType))
                        {
                            // Start of table encountered. Add previous segment to the collection
                            CloseSegment(ref segmentStart, cursor, _segment.End);
                        }
                        cursor.MoveToNextContextPosition(LogicalDirection.Forward);

                        if (typeof(Run).IsAssignableFrom(elementType) ||
                            typeof(BlockUIContainer).IsAssignableFrom(elementType))
                        {// Skip the whole element - it dos not contain Tables, Figures or Floaters
                            cursor.MoveToElementEdge(ElementEdge.AfterEnd);
                        }
                    }
                    else if (nextContext == TextPointerContext.ElementEnd)
                    {
                        Type elementType = cursor.ParentType;
                        if (typeof(TableCell).IsAssignableFrom(elementType) ||
                            typeof(Floater).IsAssignableFrom(elementType) ||
                            typeof(Figure).IsAssignableFrom(elementType))
                        {
                            // End of cell encountered. Add the previous segment to the collection
                            CloseSegment(ref segmentStart, cursor, _segment.End);
                        }

                        // Skip the closing tag
                        cursor.MoveToNextContextPosition(LogicalDirection.Forward);
                    }
                    else if (nextContext == TextPointerContext.Text || nextContext == TextPointerContext.EmbeddedElement)
                    {
                        // Open new segment if it was not opened already
                        OpenSegment(ref segmentStart, cursor);

                        // Skip the text run
                        cursor.MoveToNextContextPosition(LogicalDirection.Forward);
                    }
                    else
                    {
                        Invariant.Assert(false, "unexpected TextPointerContext");
                    }
                }

                // Close the last segment
                CloseSegment(ref segmentStart, cursor, _segment.End);
            }



            // Opens a segment for the following portion
            private void OpenSegment(ref ITextPointer segmentStart, ITextPointer cursor)
            {
                if (segmentStart == null)
                {
                    // Create normalized position for the segment start
                    segmentStart = cursor.GetInsertionPosition(LogicalDirection.Forward);
                }
            }


            // Adds individual segment to a collection
            private void CloseSegment(ref ITextPointer segmentStart, ITextPointer cursor, ITextPointer end)
            {
                if (segmentStart != null)
                {
                    // Check for going beyond the end
                    if (cursor.CompareTo(end) > 0)
                    {
                        cursor = end;
                    }

                    // Create normalized position for the segment end
                    ITextPointer segmentEnd = cursor.GetInsertionPosition(LogicalDirection.Backward);

                    // Add segment to the collection if it is not empty
                    if (segmentStart.CompareTo(segmentEnd) < 0)
                    {
                        _contentSegments.Add(new TextSegment(segmentStart, segmentEnd));
                    }

                    // Close the previous segment
                    segmentStart = null;
                }
            }


            #endregion Private methods

            #region Protected Properties

            /// <summary>
            /// Get the geometry that defines this shape
            /// </summary>
            protected override Geometry DefiningGeometry
            {
                get
                {
                    //on fixed document the highlights are drawn in a different way
                    if (_isFixedContainer)
                        return Geometry.Empty;

                    Debug.Assert(TopOwner != null, "invalid TopOwner");
                    ITextView parentView = TextSelectionHelper.GetDocumentPageTextView(TopOwner.Range.Start.CreatePointer(LogicalDirection.Forward));
                    Debug.Assert(parentView != null, "geometry parent text view not found");
                    GeometryGroup geometry = new GeometryGroup();

                    if (TopOwner.HighlightContent)
                    {
                        foreach (TextSegment segment in _contentSegments)
                        {
                            GetSegmentGeometry(geometry, segment, parentView);
                        }
                    }
                    else
                    {
                        GetSegmentGeometry(geometry, _segment, parentView);
                    }

                    //reset render transformation of the TopOwner
                    UIElement uie = TopOwner as UIElement;
                    if (uie != null)
                        uie.RenderTransform = Transform.Identity;

                    return geometry;
                }
            }


            #endregion Protected Properties

            //------------------------------------------------------
            //
            //  Internal properties
            //
            //------------------------------------------------------

            #region Internal Properties

            internal TextSegment Segment
            {
                get
                {
                    return _segment;
                }
            }

            /// <summary>
            /// returns the IHighlightRange that is currently drawn on top of this TextSegment 
            /// </summary>
            internal IHighlightRange TopOwner
            {
                get
                {
                    if (_activeOwners.Count != 0)
                        return _activeOwners[0];
                    else
                        //TBD implement Z order
                        return _owners.Count > 0 ? _owners[0] : null;
                }
            }

            #endregion Internal Properties

            //------------------------------------------------------
            //
            //  Private properties
            //
            //------------------------------------------------------

            #region Private Properties

            /// <summary>
            /// Creates a background Brush for this segment that reflects the properties
            /// and Z order of the owners
            /// </summary>
            private Brush OwnerColor
            {
                get
                {
                    if (_activeOwners.Count != 0)
                        return new SolidColorBrush(_activeOwners[0].SelectedBackground);
                    else
                        //TBD implement Z order
                        return _owners.Count > 0 ? new SolidColorBrush(_owners[0].Background) : null;
                }
            }


            #endregion Private Properties

            //------------------------------------------------------
            //
            //  Private fields
            //
            //------------------------------------------------------

            #region Private fields

            private TextSegment _segment;
            private List<TextSegment> _contentSegments = new List<TextSegment>(1);
            private readonly List<IHighlightRange> _owners;
            private List<IHighlightRange> _activeOwners = new List<IHighlightRange>();
            private IHighlightRange _cachedTopOwner = null;
            private bool _isFixedContainer;

            #endregion Private fields
        }
        #endregion Private Types

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        /// <summary>
        /// A list of all HiglightSegments ordered by position
        /// </summary>
        List<HighlightSegment> _segments;
        bool _isFixedContainer = false;

        #endregion Private Fields
    }
}
