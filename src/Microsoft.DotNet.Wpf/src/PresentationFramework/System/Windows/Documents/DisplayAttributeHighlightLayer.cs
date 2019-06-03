// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Highlight rendering for IME compositions.
//

using System;
using System.Diagnostics;
using System.Collections;
using System.Windows.Media;

namespace System.Windows.Documents
{
#if UNUSED_IME_HIGHLIGHT_LAYER
    // Highlight rendering for IME compositions.
    internal class DisplayAttributeHighlightLayer : HighlightLayer
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        // Constructor.
        internal DisplayAttributeHighlightLayer()
        {
            // No point in delay allocating _attributeRanges -- we don't
            // create a DisplayAttributeHighlightLayer unless we have
            // at least one highlight to add.
            _attributeRanges = new ArrayList(1);
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
            AttributeRange attributeRange;
            object value;

            value = DependencyProperty.UnsetValue;

            attributeRange = GetRangeAtPosition(textPosition, direction);
            if (attributeRange != null)
            {
                value = attributeRange.TextDecorations;
            }

            return value;
        }

        // Returns true iff the indicated content has scoping highlights.
        internal override bool IsContentHighlighted(StaticTextPointer textPosition, LogicalDirection direction)
        {
            return (GetRangeAtPosition(textPosition, direction) != null);
        }

        // Returns the position of the next highlight start or end in an
        // indicated direction, or null if there is no such position.
        internal override StaticTextPointer GetNextChangePosition(StaticTextPointer textPosition, LogicalDirection direction)
        {
            StaticTextPointer transitionPosition;
            AttributeRange attributeRange;
            int i;

            transitionPosition = StaticTextPointer.Null;

            // Use a simple iterative search since we don't ever have
            // more than a handful of attributes in a composition.

            if (direction == LogicalDirection.Forward)
            {
                for (i = 0; i < _attributeRanges.Count; i++)
                {
                    attributeRange = (AttributeRange)_attributeRanges[i];

                    if (attributeRange.Start.CompareTo(attributeRange.End) != 0)
                    {
                        if (textPosition.CompareTo(attributeRange.Start) < 0)
                        {
                            transitionPosition = attributeRange.Start.CreateStaticPointer();
                            break;
                        }
                        else if (textPosition.CompareTo(attributeRange.End) < 0)
                        {
                            transitionPosition = attributeRange.End.CreateStaticPointer();
                            break;
                        }
                    }
                }
            }
            else
            {
                for (i = _attributeRanges.Count - 1; i >= 0; i--)
                {
                    attributeRange = (AttributeRange)_attributeRanges[i];

                    if (attributeRange.Start.CompareTo(attributeRange.End) != 0)
                    {
                        if (textPosition.CompareTo(attributeRange.End) > 0)
                        {
                            transitionPosition = attributeRange.End.CreateStaticPointer();
                            break;
                        }
                        else if (textPosition.CompareTo(attributeRange.Start) > 0)
                        {
                            transitionPosition = attributeRange.Start.CreateStaticPointer();
                            break;
                        }
                    }
                }
            }

            return transitionPosition;
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
                return typeof(FrameworkTextComposition);
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
        //
        // This event is never fired by this class, so no implementation
        // is needed.  (We rely on Highlights to automatically raise a
        // change event when the layer is added.)
        internal override event HighlightChangedEventHandler Changed
        {
            add
            {
            }

            remove
            {
            }
        }

        #endregion Internal Events

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        // Adds a range to this collection.  Additions are expected to be in
        // order, and non-overlapping.
        internal void Add(ITextPointer start, ITextPointer end, TextDecorationCollection textDecorations)
        {
            // We expect all ranges to be added in order....
            Debug.Assert(_attributeRanges.Count == 0 || ((AttributeRange)_attributeRanges[_attributeRanges.Count - 1]).End.CompareTo(start) <= 0);

            _attributeRanges.Add(new AttributeRange(start, end, textDecorations));
        }

        #endregion Internal Methods

        // Returns the AttributeRange covering specified content, or null
        // if no such range exists.
        private AttributeRange GetRangeAtPosition(StaticTextPointer textPosition, LogicalDirection direction)
        {
            int i;
            AttributeRange attributeRange;
            AttributeRange attributeRangeAtPosition;

            // Use a simple iterative search since we don't ever have
            // more than a handful of attributes in a composition.

            attributeRangeAtPosition = null;

            if (direction == LogicalDirection.Forward)
            {
                for (i = 0; i < _attributeRanges.Count; i++)
                {
                    attributeRange = (AttributeRange)_attributeRanges[i];

                    if (attributeRange.Start.CompareTo(attributeRange.End) != 0)
                    {
                        if (textPosition.CompareTo(attributeRange.Start) < 0)
                        {
                            break;
                        }
                        else if (textPosition.CompareTo(attributeRange.End) < 0)
                        {
                            attributeRangeAtPosition = attributeRange;
                            break;
                        }
                    }
                }
            }
            else
            {
                for (i = _attributeRanges.Count - 1; i >= 0; i--)
                {
                    attributeRange = (AttributeRange)_attributeRanges[i];

                    if (attributeRange.Start.CompareTo(attributeRange.End) != 0)
                    {
                        if (textPosition.CompareTo(attributeRange.End) > 0)
                        {
                            break;
                        }
                        else if (textPosition.CompareTo(attributeRange.Start) > 0)
                        {
                            attributeRangeAtPosition = attributeRange;
                            break;
                        }
                    }
                }
            }

            return attributeRangeAtPosition;
        }

        //------------------------------------------------------
        //
        //  Private Types
        //
        //------------------------------------------------------

        #region Private Types

        // A run of a text with associated TextDecorations.
        // Because we rely on TextServices for the start/end bounds,
        // over time a range may collapse to empty.  In this case
        // it should be hidden from callers.
        private class AttributeRange
        {
            internal AttributeRange(ITextPointer start, ITextPointer end, TextDecorationCollection textDecorations)
            {
                // AttributeRange should not be crossed later.
                Debug.Assert((start.LogicalDirection != LogicalDirection.Forward) || (end.LogicalDirection != LogicalDirection.Backward));

                _start = start;
                _end = end;
                _textDecorations = textDecorations;
            }

            internal ITextPointer Start
            {
                get
                {
                    return _start;
                }
            }

            internal ITextPointer End
            {
                get
                {
                    return _end;
                }
            }

            internal TextDecorationCollection TextDecorations
            {
                get
                {
                    return _textDecorations;
                }
            }

            // Start position of the run.
            private readonly ITextPointer _start;

            // End position of the run.
            private readonly ITextPointer _end;

            // TextDecorations used to highlight the run.
            private readonly TextDecorationCollection _textDecorations;
        }

        #endregion Private Types

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // Array of AttributeRange highlights.
        private readonly ArrayList _attributeRanges;

        #endregion Private Fields
    }
#endif
}
