// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description:
//     TextAnchor represents a set of TextSegments that are part of an annotation's
//     attached anchor.  The TextSegments do not overlap and are ordered.  
//
//     We cannot use TextRange for this purpose because we need to represent sets of
//     TextSegments that are not valid TextRanges (such as non-rectangular regions of
//     a table).
//

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Documents;

using MS.Internal;

namespace System.Windows.Annotations
{
    /// <summary>
    /// </summary>
    public sealed class TextAnchor
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Creates an empty TextAnchor.  If left empty it will be invalid for most operations.
        /// </summary>
        internal TextAnchor()
        {
        }

        /// <summary>
        /// Creates a clone of the passed in TextAnchor.
        /// </summary>
        /// <param name="anchor"></param>
        internal TextAnchor(TextAnchor anchor)
        {
            Invariant.Assert(anchor != null, "Anchor to clone is null.");

            foreach (TextSegment segment in anchor.TextSegments)
            {
                _segments.Add(new TextSegment(segment.Start, segment.End));
            }
        }

        /*
         * Code used to trim text segments for alternative display of sticky note anchors
         * 
        /// <summary>
        /// ctor that initializes the TextSegments array by cloning and trimming the input segment Array
        /// </summary>
        /// <param name="segments">input segment</param>
        /// <remarks>This is used to convert a TextRange into TextAnchor.
        /// Input segments must be ordered and non overlapping</remarks>
        internal TextAnchor(IList<TextSegment> segments)
        {
            if (segments == null)
                return;

            ITextPointer lastPointer = null;
            for (int i = 0; i < segments.Count; i++)
            {
                Invariant.Assert((lastPointer == null) || (lastPointer.CompareTo(segments[i].Start) <= 0), "overlapped segments found");
                TextSegment newSegment = TextAnchor.Trim(segments[i]);
                if (newSegment.IsNull)
                    continue;

                _segments.Add(newSegment);
                lastPointer = newSegment.End;
            }
        }
        */

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// Determines if the text pointer is contained by one of the
        /// anchor's TextSegment.s
        /// </summary>
        /// <param name="textPointer">text pointer to test</param>
        internal bool Contains(ITextPointer textPointer)
        {
            if (textPointer == null)
            {
                throw new ArgumentNullException("textPointer");
            }

            if (textPointer.TextContainer != this.Start.TextContainer)
            {
                throw new ArgumentException(SR.Get(SRID.NotInAssociatedTree, "textPointer"));
            }

            // Correct position normalization on range boundary so that
            // our test would not depend on what side of formatting tags
            // pointer is located.
            if (textPointer.CompareTo(this.Start) < 0)
            {
                textPointer = textPointer.GetInsertionPosition(LogicalDirection.Forward);
            }
            else if (textPointer.CompareTo(this.End) > 0)
            {
                textPointer = textPointer.GetInsertionPosition(LogicalDirection.Backward);
            }

            // Check if at least one segment contains this position.
            for (int i = 0; i < _segments.Count; i++)
            {
                if (_segments[i].Contains(textPointer))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Add a text segment with the specified text pointers.
        /// </summary>
        /// <param name="start">start pointer for the new text segment</param>
        /// <param name="end">end pointer for the new text segment</param>
        internal void AddTextSegment(ITextPointer start, ITextPointer end)
        {
            Invariant.Assert(start != null, "Non-null start required to create segment.");
            Invariant.Assert(end != null, "Non-null end required to create segment.");

            TextSegment newSegment = CreateNormalizedSegment(start, end);

            InsertSegment(newSegment);
        }

        /// <summary>
        /// Returns the hash code for this anchor.  Implementation is required
        /// because Equals was overriden.
        /// </summary>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        /// Determines if two TextAnchors are equal - they contain
        /// the same number of segments and the segments all have the
        /// same start and ends.
        /// </summary>
        /// <param name="obj">the other TextAnchor to compare to</param>
        public override bool Equals(object obj)
        {
            TextAnchor other = obj as TextAnchor;

            if (other == null)
                return false;

            if (other._segments.Count != this._segments.Count)
                return false;

            for (int i = 0; i < _segments.Count; i++)
            {
                if ((_segments[i].Start.CompareTo(other._segments[i].Start) != 0) ||
                    (_segments[i].End.CompareTo(other._segments[i].End) != 0))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Determines if there is any overlap between this anchor and the passed
        /// in set of TextSegments.
        /// </summary>
        /// <param name="textSegments">set of segments to test against</param>
        internal bool IsOverlapping(ICollection<TextSegment> textSegments)
        {
            Invariant.Assert(textSegments != null, "TextSegments must not be null.");

            textSegments = SortTextSegments(textSegments, false);

            TextSegment ourSegment, theirSegment;

            IEnumerator<TextSegment> ourEnumerator = _segments.GetEnumerator();
            IEnumerator<TextSegment> theirEnumerator = textSegments.GetEnumerator();
            bool moreOurs = ourEnumerator.MoveNext();
            bool moreTheirs = theirEnumerator.MoveNext();

            while (moreOurs && moreTheirs)
            {
                ourSegment = ourEnumerator.Current;
                theirSegment = theirEnumerator.Current;

                //special case for 0 length segments
                if (theirSegment.Start.CompareTo(theirSegment.End) == 0)
                {
                    // Check boundaries. If theirSegment is at the beginning/end of ourSegment
                    // we check the LogicalDirection. Thus we can handle end of lines, end of pages,
                    // bidiractional texts (arabic etc)

                    // If their segment is at the start of ourSegment
                    // We have overlapping if the direction of theirSegment.Start is toward ourSegment
                    if ((ourSegment.Start.CompareTo(theirSegment.Start) == 0) &&
                        (theirSegment.Start.LogicalDirection == LogicalDirection.Forward))
                        return true;

                    // If their segment is at the end of ourSegment
                    // We have overlapping if the direction of theirSegment.End is toward ourSegment
                    if ((ourSegment.End.CompareTo(theirSegment.End) == 0) &&
                    (theirSegment.End.LogicalDirection == LogicalDirection.Backward))
                        return true;
                }

                // our segment is after their segment, so try the next of their segments
                if (ourSegment.Start.CompareTo(theirSegment.End) >= 0)
                {
                    moreTheirs = theirEnumerator.MoveNext(); // point to the next of their segments
                    continue;
                }

                // our segment is before their segment so try next of our segments
                if (ourSegment.End.CompareTo(theirSegment.Start) <= 0)
                {
                    moreOurs = ourEnumerator.MoveNext(); // point to the next of our segments
                    continue;
                }

                // at this point we know for sure that there is some overlap
                return true;
            }

            // no overlaps found
            return false;
        }

        /// <summary>
        /// Calculate the 'exclusive' union of the two anchors.  Exclusive means none of the segments
        /// contributed by either anchor are allowed to overlap.  The method will throw an exception if
        /// they do.  This method modifies the first anchor passed in.  Callers should assign the
        /// result of this method to the anchor they passed in.
        /// </summary>
        internal static TextAnchor ExclusiveUnion(TextAnchor anchor, TextAnchor otherAnchor)
        {
            Invariant.Assert(anchor != null, "anchor must not be null.");
            Invariant.Assert(otherAnchor != null, "otherAnchor must not be null.");

            foreach (TextSegment segment in otherAnchor.TextSegments)
            {
                anchor.InsertSegment(segment);
            }

            return anchor;
        }

        /// <summary>
        /// Modifies the passed in TextAnchor to contain its relative 
        /// complement to the set of text segments passed in.  The resulting
        /// TextAnchor contains those segments or portions of segments that do
        /// not overlap with the passed in segments in anyway.  If after trimming
        /// the anchor has no more segments, null is returned instead.  Callers
        /// should assign the result of this method to the anchor they passed in.
        /// </summary>
        /// <param name="anchor">the anchor to trim</param>
        /// <param name="textSegments">the text segments to calculate relative complement with</param>
        /// <remarks>Note: textSegments is expected to be ordered and contain no overlapping segments</remarks>
        internal static TextAnchor TrimToRelativeComplement(TextAnchor anchor, ICollection<TextSegment> textSegments)
        {
            Invariant.Assert(anchor != null, "Anchor must not be null.");
            Invariant.Assert(textSegments != null, "TextSegments must not be null.");

            textSegments = SortTextSegments(textSegments, true);

            IEnumerator<TextSegment> enumerator = textSegments.GetEnumerator();
            bool hasMore = enumerator.MoveNext();
            int currentIndex = 0;
            TextSegment current;
            TextSegment otherSegment = TextSegment.Null;
            while (currentIndex < anchor._segments.Count && hasMore)
            {
                Invariant.Assert(otherSegment.Equals(TextSegment.Null) || otherSegment.Equals(enumerator.Current) || otherSegment.End.CompareTo(enumerator.Current.Start) <= 0, "TextSegments are overlapping or not ordered.");

                current = anchor._segments[currentIndex];
                otherSegment = enumerator.Current;

                // Current segment is after other segment, no overlap
                // Also, done with the other segment, move to the next one
                if (current.Start.CompareTo(otherSegment.End) >= 0)
                {
                    hasMore = enumerator.MoveNext();
                    continue;  // No increment, still processing the current segment
                }

                // Current segment starts after other segment starts and ...
                if (current.Start.CompareTo(otherSegment.Start) >= 0)
                {
                    // ends before other segment ends, complete overlap, remove the segment
                    if (current.End.CompareTo(otherSegment.End) <= 0)
                    {
                        anchor._segments.RemoveAt(currentIndex);
                        continue;  // No increment, happens implicitly because of the removal
                    }
                    else
                    {
                        // ends after other segment, first portion of current overlaps, 
                        // create new segment from end of other segment to end of current
                        anchor._segments[currentIndex] = CreateNormalizedSegment(otherSegment.End, current.End);
                        // Done with the other segment, move to the next one
                        hasMore = enumerator.MoveNext();
                        continue; // No increment, need to process just created segment
                    }
                }
                // Current segment starts before other segment starts and ...
                else
                {
                    // ends after it starts, first portion of current does not overlap,
                    // create new segment for that portion
                    if (current.End.CompareTo(otherSegment.Start) > 0)
                    {
                        anchor._segments[currentIndex] = CreateNormalizedSegment(current.Start, otherSegment.Start);
                        // If there's any portion of current after other segment, create a new segment for that which
                        // will be the next one processed
                        if (current.End.CompareTo(otherSegment.End) > 0)
                        {
                            // Overlap ends before current segment's end, we create a new segment with the remainder of current segment
                            anchor._segments.Insert(currentIndex + 1, CreateNormalizedSegment(otherSegment.End, current.End));
                            // Done with the other segment, move to the next one
                            hasMore = enumerator.MoveNext();
                        }
                    }
                    // ends before it starts, current is completely before other, no overlap, do nothing
                }

                currentIndex++;
            }

            if (anchor._segments.Count > 0)
                return anchor;
            else
                return null;
        }

        /// <summary>
        /// Modifies the text anchor's TextSegments so all of them
        /// overlap with the passed in text segments.  This is used 
        /// for instance to clamp a TextAnchor to a set of visible 
        /// text segments.  If after trimming the anchor has no more 
        /// segments, null is returned instead.  Callers should 
        /// assign the result of this method to the anchor they 
        /// passed in.
        /// </summary>
        /// <remarks>
        /// Note: This method assumes textSegments is ordered and do not overlap amongs themselves
        /// 
        /// The target of the method is to trim this anchor's segments to overlap with the passed in segments.
        /// The loop handles the following three cases - 
        /// 1. Current segment is after other segment, the other segment doesn't contribute at all, we move to the next other segment
        /// 2. Current segment is before other segment, no overlap, remove current segment
        /// 3. Current segment starts before other segment, and ends after other segment begins, 
        ///    therefore the portion from current's start to other's start should be trimmed
        /// 4. Current segment starts in the middle of other segment, two possibilities
        ///      a. current segment is completely within other segment, the whole segment overlaps 
        ///         so we move on to the next current segment
        ///      b. current segment ends after other segment ends, we split current into the 
        ///         overlapped portion and the remainder which will be looked at separately
        /// </remarks>
        /// <param name="anchor">the anchor to trim</param>
        /// <param name="textSegments">collection of text segments to intersect with</param>
        internal static TextAnchor TrimToIntersectionWith(TextAnchor anchor, ICollection<TextSegment> textSegments)
        {
            Invariant.Assert(anchor != null, "Anchor must not be null.");
            Invariant.Assert(textSegments != null, "TextSegments must not be null.");

            textSegments = SortTextSegments(textSegments, true);
            TextSegment currentSegment, otherSegment = TextSegment.Null;

            int current = 0;
            IEnumerator<TextSegment> enumerator = textSegments.GetEnumerator();
            bool hasMore = enumerator.MoveNext();

            while (current < anchor._segments.Count && hasMore)
            {
                Invariant.Assert(otherSegment.Equals(TextSegment.Null) || otherSegment.Equals(enumerator.Current) || otherSegment.End.CompareTo(enumerator.Current.Start) <= 0, "TextSegments are overlapping or not ordered.");

                currentSegment = anchor._segments[current];
                otherSegment = enumerator.Current;

                // Current segment is after other segment, so try the next other segment
                if (currentSegment.Start.CompareTo(otherSegment.End) >= 0)
                {
                    hasMore = enumerator.MoveNext(); // point to the next other
                    continue; // Do not increment, we are still on the same current
                }

                // Current segment is before other segment, no overlap so remove it and continue
                if (currentSegment.End.CompareTo(otherSegment.Start) <= 0)
                {
                    anchor._segments.RemoveAt(current);
                    continue; // Do not increment, it happens implicitly because of the remove
                }

                //
                // We know from here down that there is some overlap.
                //

                // Current starts before the other segment and ends after other segment begins, the first portion of current segment doesn't overlap so we remove it
                if (currentSegment.Start.CompareTo(otherSegment.Start) < 0)
                {
                    anchor._segments[current] = CreateNormalizedSegment(otherSegment.Start, currentSegment.End);
                    continue;  // Do not increment, we need to look at this just created segment
                }
                // Current segment begins in the middle of other segment...
                else
                {
                    // and ends after other segment does, we split current into the portion that is overlapping and the remainder
                    if (currentSegment.End.CompareTo(otherSegment.End) > 0)
                    {
                        anchor._segments[current] = CreateNormalizedSegment(currentSegment.Start, otherSegment.End);
                        // This segment will be the first one looked at next
                        anchor._segments.Insert(current + 1, CreateNormalizedSegment(otherSegment.End, currentSegment.End));
                        hasMore = enumerator.MoveNext();
                    }
                    // and ends at the same place as other segment, its completely overlapping, we move on to the next other
                    else if (currentSegment.End.CompareTo(otherSegment.End) == 0)
                    {
                        hasMore = enumerator.MoveNext();
                    }
                    // and ends within other segment, its completely overlapping, but we aren't done with other so we just continue
                }

                current++;
            }

            // If we finished and there are no more other segments, then any remaining segments
            // in our list must not overlap, so we remove them.
            if (!hasMore && current < anchor._segments.Count)
            {
                anchor._segments.RemoveRange(current, anchor._segments.Count - current);
            }

            if (anchor._segments.Count == 0)
                return null;
            else
                return anchor;
        }

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// The start of the bounding range of this TextAnchor.
        /// </summary>
        public ContentPosition BoundingStart
        {
            get
            {
                return Start as ContentPosition;
            }
        }

        /// <summary>
        /// The end of the bounding range of this TextAnchor.
        /// </summary>
        public ContentPosition BoundingEnd
        {
            get
            {
                return End as ContentPosition;
            }
        }


        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        /// <summary>
        /// The start pointer of the first segment in the TextAnchor
        /// </summary>
        internal ITextPointer Start
        {
            get
            {
                return _segments.Count > 0 ? _segments[0].Start : null;
            }
        }

        /// <summary>
        /// The end pointer of the last segment in the TextAnchor
        /// </summary>
        internal ITextPointer End
        {
            get
            {
                return _segments.Count > 0 ? _segments[_segments.Count - 1].End : null;
            }
        }

        /// <summary>
        /// Returns whether or not this text anchor is empty - meaning
        /// it has one text segment whose start and end are the same.
        /// </summary>
        internal bool IsEmpty
        {
            get
            {
                return (_segments.Count == 1 && (object)_segments[0].Start == (object)_segments[0].End);
            }
        }

        /// <summary>
        /// Returns a concatenation of the text for each of this anchor's
        /// TextSegments.
        /// </summary>
        internal string Text
        {
            get
            {
                // Buffer for building a resulting plain text
                StringBuilder textBuffer = new StringBuilder();

                for (int i = 0; i < _segments.Count; i++)
                {
                    textBuffer.Append(TextRangeBase.GetTextInternal(_segments[i].Start, _segments[i].End));
                }

                return textBuffer.ToString();
            }
        }

        /// <summary>
        /// Returns a read only collection of this anchor's TextSegments.
        /// </summary>
        internal ReadOnlyCollection<TextSegment> TextSegments
        {
            get
            {
                return _segments.AsReadOnly();
            }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// Sorts a list of text segments by their Start pointer first then End pointer.
        /// Used because list of TextSegments from a TextView are not guaranteed to be sorted
        /// but in most cases they are.  
        /// Note: In most cases the set of segments is of count 1 and this method is a no-op.
        /// In the majority of other cases the number of segments is less than 5.
        /// In extreme cases (such as a table with many, many columns and each cell
        /// in a row being split across pages) you may have more than 5 segments
        /// but this is very rare.
        /// </summary>
        /// <param name="textSegments">segments to be sorted</param>
        /// <param name="excludeZeroLength">We've seen 0 length segments in the TextView that overlap other segments
        /// this will break our algorithm, so we remove them (excludeZeroLength = true). When we calculate
        /// IsOverlapping 0-length segments are OK - then excludeZeroLength is false</param>
        private static ICollection<TextSegment> SortTextSegments(ICollection<TextSegment> textSegments, bool excludeZeroLength)
        {
            Invariant.Assert(textSegments != null, "TextSegments must not be null.");

            List<TextSegment> orderedList = new List<TextSegment>(textSegments.Count);
            orderedList.AddRange(textSegments);

            if (excludeZeroLength)
            {
                //remove 0 length segments - work around for a bug in MultiPageTextView
                for (int i = orderedList.Count - 1; i >= 0; i--)
                {
                    TextSegment segment = orderedList[i];
                    if (segment.Start.CompareTo(segment.End) >= 0)
                    {
                        //remove that one
                        orderedList.Remove(segment);
                    }
                }
            }

            // If there are 0 or 1 segments, no need to sort, just return the original collection
            if (orderedList.Count > 1)
            {
                orderedList.Sort(new TextSegmentComparer());
            }

            return orderedList;
        }


        /// <summary>
        /// Inserts a segment into this anchor in the right order.  If the new segment
        /// overlaps with existing anchors it throws an exception.
        /// </summary>
        private void InsertSegment(TextSegment newSegment)
        {
            int i = 0;
            for (; i < _segments.Count; i++)
            {
                if (newSegment.Start.CompareTo(_segments[i].Start) < 0)
                    break;
            }

            // Make sure it starts after the one its being put behind
            if (i > 0 && newSegment.Start.CompareTo(_segments[i - 1].End) < 0)
                throw new InvalidOperationException(SR.Get(SRID.TextSegmentsMustNotOverlap));

            // Make sure it ends before the one its being put ahead of
            if (i < _segments.Count && newSegment.End.CompareTo(_segments[i].Start) > 0)
                throw new InvalidOperationException(SR.Get(SRID.TextSegmentsMustNotOverlap));

            _segments.Insert(i, newSegment);
        }

        /// <summary>
        /// Creates a new segment with the specified pointers, but first
        /// normalizes them to make sure they are on insertion positions.
        /// </summary>
        /// <param name="start">start of the new segment</param>
        /// <param name="end">end of the new segment</param>
        private static TextSegment CreateNormalizedSegment(ITextPointer start, ITextPointer end)
        {
            // Normalize the segment
            if (start.CompareTo(end) == 0)
            {
                // When the range is empty we must keep it that way during normalization
                if (!TextPointerBase.IsAtInsertionPosition(start, start.LogicalDirection))
                {
                    start = start.GetInsertionPosition(start.LogicalDirection);
                    end = start;
                }
            }
            else
            {
                if (!TextPointerBase.IsAtInsertionPosition(start, start.LogicalDirection))
                {
                    start = start.GetInsertionPosition(LogicalDirection.Forward);
                }
                if (!TextPointerBase.IsAtInsertionPosition(end, start.LogicalDirection))
                {
                    end = end.GetInsertionPosition(LogicalDirection.Backward);
                }

                // Collapse range in case of overlapped normalization result
                if (start.CompareTo(end) >= 0)
                {
                    // The range is effectuvely empty, so collapse it to single pointer instance
                    if (start.LogicalDirection == LogicalDirection.Backward)
                    {
                        // Choose a position normalized backward,
                        start = end.GetFrozenPointer(LogicalDirection.Backward);

                        // NOTE that otherwise we will use start position,
                        // which is oriented and normalizd Forward
                    }
                    end = start;
                }
            }

            return new TextSegment(start, end);
        }

        //
        // Code used to trim text segments for alternative display of sticky note anchors.
        // 
        ///// <summary>
        ///// Trims certain whitespace off ends of segments if they fit certain
        ///// conditions - such as being inside of an embedded element.
        ///// Returns a whole new TextSegment that's been trimmed or TextSegment.Null
        ///// if the trimming results in a non-existent TextSegment.
        ///// </summary>
        //private static TextSegment Trim(TextSegment segment)
        //{
        //    ITextPointer cursor = segment.Start.CreatePointer();
        //    ITextPointer segmentStart = null;

        //    TextPointerContext nextContext = cursor.GetPointerContext(LogicalDirection.Forward);
        //    while ((cursor.CompareTo(segment.End) < 0) &&
        //        (nextContext != TextPointerContext.Text) &&
        //        (nextContext != TextPointerContext.EmbeddedElement))
        //    {
        //        // Simply skip all other opening tags
        //        cursor.MoveToNextContextPosition(LogicalDirection.Forward);
        //        nextContext = cursor.GetPointerContext(LogicalDirection.Forward);
        //    }

        //    while (cursor.CompareTo(segment.End) >= 0)
        //        return TextSegment.Null;

        //    segmentStart = cursor;
        //    cursor = segment.End.CreatePointer();

        //    nextContext = cursor.GetPointerContext(LogicalDirection.Backward);
        //    while ((cursor.CompareTo(segmentStart) > 0) &&
        //        (nextContext != TextPointerContext.Text) &&
        //        (nextContext != TextPointerContext.EmbeddedElement))
        //    {
        //        cursor.MoveToNextContextPosition(LogicalDirection.Backward);
        //        nextContext = cursor.GetPointerContext(LogicalDirection.Backward);
        //    }

        //    return segmentStart.CompareTo(cursor) < 0 ? new TextSegment(segmentStart, cursor) : TextSegment.Null;
        //}
        //

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // List of text segments for this anchor
        private List<TextSegment> _segments = new List<TextSegment>(1);

        #endregion Private Fields

        //------------------------------------------------------
        //
        //  Private Classes
        //
        //------------------------------------------------------

        #region Private Classes

        /// <summary>
        /// Simple comparer class that sorts TextSegments by their Start pointers.  
        /// If Start pointers are the same, then they are sorted by their End pointers.
        /// Null is sorted as less than a non-null result.
        /// </summary>
        private class TextSegmentComparer : IComparer<TextSegment>
        {
            /// <summary>
            /// All comparisons are done a segments Start pointer. If
            /// those are the same, then the End pointers are compared.
            /// Returns 0 if x is == to y; -1 if x is less than y; 1 if x is greater than y.
            /// If x is null and y is not, returns -1; if y is null and x is not, returns 1.
            /// </summary>
            public int Compare(TextSegment x, TextSegment y)
            {
                if (x.Equals(TextSegment.Null))
                {
                    // Both are null
                    if (y.Equals(TextSegment.Null))
                        return 0;
                    // x is null but y is not
                    else
                        return -1;
                }
                else
                {
                    // x is not null but y is
                    if (y.Equals(TextSegment.Null))
                        return 1;
                    else
                    {
                        int retVal = x.Start.CompareTo(y.Start);
                        // If starts are different, return their comparison
                        if (retVal != 0)
                            return retVal;
                        // Otherwise return the comparison of the ends
                        else
                            return x.End.CompareTo(y.End);
                    }
                }
            }
        }

        #endregion Private Classes
    }
}
