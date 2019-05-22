// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 
// Description: Highlight rendering for the TextSelection.
//

using System;
using MS.Internal;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace System.Windows.Documents
{
    // Highlight rendering for the TextSelection.
    internal class TextSelectionHighlightLayer : HighlightLayer
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        // Static constructor.
        static TextSelectionHighlightLayer()
        {
            _selectedValue = new object();
        }

        // Constructor.
        internal TextSelectionHighlightLayer(ITextSelection selection)
        {
            _selection = selection;
            _selection.Changed += new EventHandler(OnSelectionChanged);

            _oldStart = _selection.Start;
            _oldEnd = _selection.End;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        // Returns the value of a property stored on scoping highlight, if any.
        //
        // If no property value is set, returns DependencyProperty.UnsetValue.
        internal override object GetHighlightValue(StaticTextPointer textPosition, LogicalDirection direction)
        {
            object value;

            if (IsContentHighlighted(textPosition, direction))
            {
                value = _selectedValue;
            }
            else
            {
                value = DependencyProperty.UnsetValue;
            }

            return value;
        }

        // Returns true iff the indicated content has scoping highlights.
        internal override bool IsContentHighlighted(StaticTextPointer textPosition, LogicalDirection direction)
        {
            int segmentCount;
            TextSegment textSegment;

            // No highlight when the selection is for interim character.
            if (_selection.IsInterimSelection)
            {
                return false;
            }

            // Check all segments of selection
            List<TextSegment> textSegments = _selection.TextSegments;
            segmentCount = textSegments.Count;
            for (int segmentIndex = 0; segmentIndex < segmentCount; segmentIndex++)
            {
                textSegment = textSegments[segmentIndex];

                if ((direction == LogicalDirection.Forward && textSegment.Start.CompareTo(textPosition) <= 0 && textPosition.CompareTo(textSegment.End) < 0) || //
                    (direction == LogicalDirection.Backward && textSegment.Start.CompareTo(textPosition) < 0 && textPosition.CompareTo(textSegment.End) <= 0))
                {
                    return true;
                }
            }
            return false;
        }

        // Returns the position of the next highlight start or end in an
        // indicated direction, or null if there is no such position.
        internal override StaticTextPointer GetNextChangePosition(StaticTextPointer textPosition, LogicalDirection direction)
        {
            StaticTextPointer transitionPosition;

            transitionPosition = StaticTextPointer.Null;

            if (!IsTextRangeEmpty(_selection) && !_selection.IsInterimSelection)
            {
                int segmentCount;
                List<TextSegment> textSegments = _selection.TextSegments;
                TextSegment textSegment;

                segmentCount = textSegments.Count;

                if (direction == LogicalDirection.Forward)
                {
                    for (int segmentIndex = 0; segmentIndex < segmentCount; segmentIndex++)
                    {
                        textSegment = textSegments[segmentIndex];

                        // Ignore empty segments.
                        // 
                        // Filtering out empty segments is a workaround.
                        // The root problem is that table selections are not
                        // always normalized, and in any case normalization
                        // is problematic because the layout system
                        // will call this code while computing layout and
                        // the normalization code depends on a clean layout.
                        //
                        // We need to:
                        // 1. Normalize table selections all the time.
                        // 2. Remove the dependency on layout from our normalization code.
                        //
                        // Then we will never have empty segments.
                        if (textSegment.Start.CompareTo(textSegment.End) != 0)
                        {
                            if (textPosition.CompareTo(textSegment.Start) < 0)
                            {
                                transitionPosition = textSegment.Start.CreateStaticPointer();
                                break;
                            }
                            else if (textPosition.CompareTo(textSegment.End) < 0)
                            {
                                transitionPosition = textSegment.End.CreateStaticPointer();
                                break;
                            }
                        }
                    }
                }
                else
                {
                    for (int segmentIndex = segmentCount - 1; segmentIndex >= 0; segmentIndex--)
                    {
                        textSegment = textSegments[segmentIndex];

                        if (textSegment.Start.CompareTo(textSegment.End) != 0)
                        {
                            if (textPosition.CompareTo(textSegment.End) > 0)
                            {
                                transitionPosition = textSegment.End.CreateStaticPointer();
                                break;
                            }
                            else if (textPosition.CompareTo(textSegment.Start) > 0)
                            {
                                transitionPosition = textSegment.Start.CreateStaticPointer();
                                break;
                            }
                        }
                    }
                }
            }

            return transitionPosition;
        }

        // This is actual implementation to make highlight change notification.
        // This is called when highlight needs to be changed without public TextSelection change event.
        internal void InternalOnSelectionChanged()
        {
            ITextPointer newStart;
            ITextPointer newEnd;
            ITextPointer invalidRangeLeftStart;
            ITextPointer invalidRangeLeftEnd;
            ITextPointer invalidRangeRightStart;
            ITextPointer invalidRangeRightEnd;
            TextSelectionHighlightChangedEventArgs args;

            // If the current seleciton is interim selection, we do not highlight it
            // and we make newStart == newEnd.
            if (!_selection.IsInterimSelection)
            {
                newStart = _selection.Start;
            }
            else
            {
                newStart = _selection.End;
            }

            newEnd = _selection.End;

            // We want to raise an event that tracks the change tightly --
            // only identifying content where the selection status actually changed.
            // This is important for render performance.
            //
            // Ex:
            // Old selection: 012<selection>345</selection>678
            // New selection: 0123<selection>456</selection>78
            //
            // Should raise (3-3), (6-6) as deltas, not (3-6).
            //

            // Get the left side invalid range.
            if (_oldStart.CompareTo(newStart) < 0)
            {
                invalidRangeLeftStart = _oldStart;
                invalidRangeLeftEnd = TextPointerBase.Min(newStart, _oldEnd);
            }
            else
            {
                invalidRangeLeftStart = newStart;
                invalidRangeLeftEnd = TextPointerBase.Min(newEnd, _oldStart);
            }

            // Get the right side invalid range.
            if (_oldEnd.CompareTo(newEnd) < 0)
            {
                invalidRangeRightStart = TextPointerBase.Max(newStart, _oldEnd);
                invalidRangeRightEnd = newEnd;
            }
            else
            {
                invalidRangeRightStart = TextPointerBase.Max(newEnd, _oldStart);
                invalidRangeRightEnd = _oldEnd;
            }

            _oldStart = newStart;
            _oldEnd = newEnd;

            if (this.Changed != null)
            {
                if (invalidRangeLeftStart.CompareTo(invalidRangeLeftEnd) != 0 || invalidRangeRightStart.CompareTo(invalidRangeRightEnd) != 0)
                {
                    args = new TextSelectionHighlightChangedEventArgs(invalidRangeLeftStart, invalidRangeLeftEnd, invalidRangeRightStart, invalidRangeRightEnd);

                    this.Changed(this, args);
                }
            }
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
                return typeof(TextSelection);
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

        // Callback for TextSelection.Moved event.
        // We use this event to trigger highlight change notifications.
        private void OnSelectionChanged(object sender, EventArgs e)
        {
            Invariant.Assert(_selection == (ITextSelection)sender);
            InternalOnSelectionChanged();
        }

        // Check the text range whether it is empty or not without normalization.
        private bool IsTextRangeEmpty(ITextRange textRange)
        {
            // We assume that TextRange.TextSegments property getter does not normalize a range,
            // thus we can avoid re-entrancy.
            Invariant.Assert(textRange._TextSegments.Count > 0);
            return textRange._TextSegments[0].Start.CompareTo(textRange._TextSegments[textRange._TextSegments.Count - 1].End) == 0;
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Types
        //
        //------------------------------------------------------

        #region Private Types

        // Argument for the Changed event, encapsulates a highlight change.
        private class TextSelectionHighlightChangedEventArgs : HighlightChangedEventArgs
        {
            // Constructor.
            internal TextSelectionHighlightChangedEventArgs(ITextPointer invalidRangeLeftStart, ITextPointer invalidRangeLeftEnd,
                                                            ITextPointer invalidRangeRightStart, ITextPointer invalidRangeRightEnd)
            {
                List<TextSegment> rangeArray;

                Invariant.Assert(invalidRangeLeftStart != invalidRangeLeftEnd || invalidRangeRightStart != invalidRangeRightEnd, "Unexpected empty range!");

                if (invalidRangeLeftStart.CompareTo(invalidRangeLeftEnd) == 0)
                {
                    rangeArray = new List<TextSegment>(1);
                    rangeArray.Add(new TextSegment(invalidRangeRightStart, invalidRangeRightEnd));
                }
                else if (invalidRangeRightStart.CompareTo(invalidRangeRightEnd) == 0)
                {
                    rangeArray = new List<TextSegment>(1);
                    rangeArray.Add(new TextSegment(invalidRangeLeftStart, invalidRangeLeftEnd));
                }
                else
                {
                    rangeArray = new List<TextSegment>(2);
                    rangeArray.Add(new TextSegment(invalidRangeLeftStart, invalidRangeLeftEnd));
                    rangeArray.Add(new TextSegment(invalidRangeRightStart, invalidRangeRightEnd));
                }

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
                    return typeof(TextSelection);
                }
            }

            // Collection of changed content ranges.
            private readonly ReadOnlyCollection<TextSegment> _ranges;
        }

        #endregion Private Types

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // TextSelection associated with this layer.
        private readonly ITextSelection _selection;

        // Previous position of the TextSelection, used to calculate deltas
        // on TextSelection.Moved events.
        private ITextPointer _oldStart;
        private ITextPointer _oldEnd;

        // Sentinel object used to tag highlights.
        private static readonly object _selectedValue;

        #endregion Private Fields
    }
}
