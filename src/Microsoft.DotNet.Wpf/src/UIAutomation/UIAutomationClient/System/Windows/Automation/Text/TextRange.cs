// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Client-side wrapper for text range.
//
//                  and moved to .Text subnamespace.
//

// PRESHARP: In order to avoid generating warnings about unkown message numbers and unknown pragmas.
#pragma warning disable 1634, 1691

using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows.Automation.Provider;
using MS.Internal.Automation;

namespace System.Windows.Automation.Text
{
    // Internal Class that wraps the IntPtr to the TextRange
    internal sealed class SafeTextRangeHandle : SafeHandle
    {
        // Called by P/Invoke when returning SafeHandles
        // (Also used by UiaCoreApi to create invalid handles.)
        internal SafeTextRangeHandle()
            : base(IntPtr.Zero, true)
        {
        }
        // No need to provide a finalizer - SafeHandle's critical finalizer will
        // call ReleaseHandle for you.
        public override bool IsInvalid
        {
            get { return handle == IntPtr.Zero; }
        }

        override protected bool ReleaseHandle()
        {
            return UiaCoreApi.UiaTextRangeRelease(handle);
        }
    }

    /// <summary>
    /// Represents a span of text in a Text Pattern container. 
    /// </summary>
#if (INTERNAL_COMPILE)
    internal class TextPatternRange
#else
    public class TextPatternRange
#endif
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        internal TextPatternRange(SafeTextRangeHandle hTextRange, TextPattern pattern)
        {
            Debug.Assert(!hTextRange.IsInvalid);
            Debug.Assert(pattern != null);

            _hTextRange = hTextRange;
            _pattern = pattern;
        }

        internal static TextPatternRange Wrap(SafeTextRangeHandle hTextRange, TextPattern pattern)
        {
            if (hTextRange.IsInvalid)
            {
                return null;
            }
            return new TextPatternRange(hTextRange, pattern);
        }

        internal static TextPatternRange [] Wrap(SafeTextRangeHandle [] hTextRanges, TextPattern pattern)
        {
            if (hTextRanges == null)
                return null;

            TextPatternRange[] ranges = new TextPatternRange[hTextRanges.Length];
            for (int i = 0; i < hTextRanges.Length; i++)
            {
                // if invalid, leave as null
                if (!hTextRanges[i].IsInvalid)
                {
                    ranges[i] = new TextPatternRange(hTextRanges[i], pattern);
                }
            }
            return ranges;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------
 
        #region Public Methods

        /// <summary>
        /// Retrieves a new range covering an identical span of text.  The new range can be manipulated independently from the original.
        /// </summary>
        /// <returns>The new range.</returns>
        public TextPatternRange Clone()
        {
            SafeTextRangeHandle hResultTextRange = UiaCoreApi.TextRange_Clone(_hTextRange);
            return Wrap(hResultTextRange, _pattern);
        }

        /// <summary>
        /// Compares this range with another range.
        /// </summary>
        /// <param name="range">A range to compare. 
        /// The range must have come from the same text provider or an InvalidArgumentException will be thrown.</param>
        /// <returns>true if both ranges span the same text.</returns>
        public bool Compare(TextPatternRange range)
        {
            ValidateRangeArgument(range, "range");
            return UiaCoreApi.TextRange_Compare(_hTextRange, range._hTextRange);
        }

        /// <summary>
        /// Compares the endpoint of this range with the endpoint of another range.
        /// </summary>
        /// <param name="endpoint">The endpoint of this range to compare.</param>
        /// <param name="targetRange">The range with the other endpoint to compare.
        /// The range must have come from the same text provider or an InvalidArgumentException will be thrown.</param>
        /// <param name="targetEndpoint">The endpoint on the other range to compare.</param>
        /// <returns>Returns &lt;0 if this endpoint occurs earlier in the text than the target endpoint. 
        /// Returns 0 if this endpoint is at the same location as the target endpoint. 
        /// Returns &gt;0 if this endpoint occurs later in the text than the target endpoint.</returns>
        public int CompareEndpoints(TextPatternRangeEndpoint endpoint, TextPatternRange targetRange, TextPatternRangeEndpoint targetEndpoint)
        {
            ValidateEndpointArgument(endpoint, "endpoint");
            ValidateRangeArgument(targetRange, "targetRange");
            ValidateEndpointArgument(targetEndpoint, "targetEndpoint");

            return UiaCoreApi.TextRange_CompareEndpoints(_hTextRange, endpoint, targetRange._hTextRange, targetEndpoint);
        }

        /// <summary>
        /// Expands the range to an integral number of enclosing units.  This could be used, for example,
        /// to guarantee that a range endpoint is not in the middle of a word.  If the range is already an
        /// integral number of the specified units then it remains unchanged.
        /// For the TextUnit.Word unit with a degenerate range if the range is immediately before a word,
        /// inside that word, or within the whitespace following that word (but not immediately before the next
        /// word) then it will expand to include that word.
        /// For the TextUnit.Format unit if the range has mixed formatting then the range starting point will
        /// be moved backwards over any text that is identically formatted to that at the start of the range
        /// and the endpoint will be move forwards over any text that is identically formatted to that at the
        /// end of the range.
        /// </summary>
        /// <param name="unit">The textual unit.</param>
        public void ExpandToEnclosingUnit(TextUnit unit)
        {
            ValidateUnitArgument(unit, "unit");

            UiaCoreApi.TextRange_ExpandToEnclosingUnit(_hTextRange, unit);
        }

        /// <summary>
        /// Searches for a subrange of text that has the specified attribute.
        /// To search the entire document use the text pattern's document range.
        /// </summary>
        /// <param name="attribute">The attribute to search for.</param>
        /// <param name="value">The value of the specified attribute to search for.  The value must be of the exact type specified for the 
        /// attribute.  For example when searching for font size you must specify the size in points as a double.
        /// If you specify the point size as an integer then you will never get any matches due to the differing types.</param>
        /// <param name="backward">true if the last occurring range should be returned instead of the first.</param>
        /// <returns>A subrange with the specified attribute, or null if no such subrange exists.</returns>
        public TextPatternRange FindAttribute(AutomationTextAttribute attribute, object value, bool backward)
        {
            Misc.ValidateArgumentNonNull(attribute, "attribute");
            Misc.ValidateArgumentNonNull(value, "value"); // no text attributes can have null as a valid value

            // Check that attribute value is of expected type...
            AutomationAttributeInfo ai;
            if(!Schema.GetAttributeInfo(attribute, out ai))
            {
                throw new ArgumentException(SR.Get(SRID.UnsupportedAttribute));
            }

            if (value.GetType() != ai.Type)
            {
                throw new ArgumentException(SR.Get(SRID.TextAttributeValueWrongType, attribute, ai.Type.Name, value.GetType().Name), "value");
            }

            // note: if we implement attributes whose values are logical elements, patterns,
            // or ranges then we'll need to unwrap the objects here before passing them on to
            // the provider.
            if (attribute == TextPattern.CultureAttribute)
            {
                if (value is CultureInfo)
                {
                    value = ((CultureInfo)value).LCID;
                }
            }

            SafeTextRangeHandle hResultTextRange = UiaCoreApi.TextRange_FindAttribute(_hTextRange, attribute.Id, value, backward);
            return Wrap(hResultTextRange, _pattern);
        }

        /// <summary>
        /// Searches for an occurrence of text within the range.
        /// </summary>
        /// <param name="text">The text to search for.</param>
        /// <param name="backward">true if the last occurring range should be returned instead of the first.</param>
        /// <param name="ignoreCase">true if case should be ignored for the purposes of comparison.</param>
        /// <returns>A subrange with the specified text, or null if no such subrange exists.</returns>
        public TextPatternRange FindText(string text, bool backward, bool ignoreCase)
        {
            // PerSharp/PreFast will flag this as warning 6507/56507: Prefer 'string.IsNullOrEmpty(text)' over checks for null and/or emptiness.
            // A null string is not should throw an ArgumentNullException while an empty string should throw an ArgumentException.
            // Therefore we can not use IsNullOrEmpty() here, suppress the warning.
            Misc.ValidateArgumentNonNull(text, "text");
#pragma warning suppress 6507
            Misc.ValidateArgument(text.Length != 0, SRID.TextMustNotBeNullOrEmpty);

            SafeTextRangeHandle hResultTextRange = UiaCoreApi.TextRange_FindText(_hTextRange, text, backward, ignoreCase);
            return Wrap(hResultTextRange, _pattern);
        }

        /// <summary>
        /// Retrieves the value of a text attribute over the entire range.
        /// </summary>
        /// <param name="attribute">The text attribute.</param>
        /// <returns>The value of the attribute across the range. 
        /// If the attribute's value varies over the range then the value is TextPattern.MixedAttributeValue</returns>
        public object GetAttributeValue(AutomationTextAttribute attribute)
        {
            Misc.ValidateArgumentNonNull(attribute, "attribute");

            AutomationAttributeInfo ai;
            if(!Schema.GetAttributeInfo(attribute, out ai))
            {
                throw new ArgumentException(SR.Get(SRID.UnsupportedAttribute));
            }

            object obj = UiaCoreApi.TextRange_GetAttributeValue(_hTextRange, attribute.Id);

            if (ai.Type.IsEnum && obj is int)
            {
                // Convert ints from COM Interop to the appropriate enum type
                obj = Enum.ToObject(ai.Type, (int)obj);
            }
            else if (obj != AutomationElement.NotSupported && ai.ObjectConverter != null)
            {
                // Use a custom converter, if needed (eg. converts LCIDs to CultureInfo)
                obj = ai.ObjectConverter(obj);
            }

            return obj;
        }

        /// <summary>
        /// Retrieves the bounding rectangles for viewable lines of the range.
        /// </summary>
        /// <returns>An array of bounding rectangles for each line or portion of a line within the client area of the text provider.
        /// No bounding rectangles will be returned for lines that are empty or scrolled out of view.  Note that even though a
        /// bounding rectangle is returned the corresponding text may not be visible due to overlapping windows.
        /// This will not return null, but may return an empty array.</returns>
        public Rect[] GetBoundingRectangles()
        {
            return UiaCoreApi.TextRange_GetBoundingRectangles(_hTextRange);
        }

        /// <summary>
        /// Retrieves the innermost element that encloses this range.
        /// </summary>
        /// <returns>An element.  Usually this element will be the one that supplied this range.
        /// However, if the text provider supports child elements such as tables or hyperlinks, then the
        /// enclosing element could be a descendant element of the text provider.
        /// </returns>
        public AutomationElement GetEnclosingElement()
        {
            return AutomationElement.Wrap(UiaCoreApi.TextRange_GetEnclosingElement(_hTextRange));
        }

        /// <summary>
        /// Retrieves the text of the range.
        /// </summary>
        /// <param name="maxLength">Specifies the maximum length of the string to return or -1 if no limit is requested.</param>
        /// <returns>The text of the range possibly truncated to the specified limit.</returns>
        public string GetText(int maxLength)
        {
            Misc.ValidateArgumentInRange(maxLength >= -1, "maxLength");
            return UiaCoreApi.TextRange_GetText(_hTextRange, maxLength);
        }

        /// <summary>
        /// Moves the range the specified number of units in the text.  Note that the text is not altered.  Instead the
        /// range spans a different part of the text.
        /// If the range is degenerate, this method tries to move the insertion point count units.  If the range is nondegenerate 
        /// and count is greater than zero, this method collapses the range at its end point, moves the resulting range forward 
        /// to a unit boundary (if it is not already at one), and then tries to move count - 1 units forward. If the range is 
        /// nondegenerate and count is less than zero, this method collapses the range at the starting point, moves the resulting 
        /// range backward to a unit boundary (if it isn't already at one), and then tries to move |count| - 1 units backward. 
        /// Thus, in both cases, collapsing a nondegenerate range, whether or not moving to the start or end of the unit following 
        /// the collapse, counts as a unit.
        /// </summary>
        /// <param name="unit">The textual unit for moving.</param>
        /// <param name="count">The number of units to move.  A positive count moves the range forward.  
        /// A negative count moves backward. A count of 0 has no effect.</param>
        /// <returns>The number of units actually moved, which can be less than the number requested if 
        /// moving the range runs into the beginning or end of the document.</returns>
        public int Move(TextUnit unit, int count)
        {
            ValidateUnitArgument(unit, "unit");
            // note: we could optimize the case of count==0 and just return 0.

            return UiaCoreApi.TextRange_Move(_hTextRange, unit, count);
        }

        /// <summary>
        /// Moves one endpoint of the range the specified number of units in the text.
        /// If the endpoint being moved crosses the other endpoint then the other endpoint
        /// is moved along too resulting in a degenerate range and ensuring the correct ordering
        /// of the endpoints. (i.e. always Start&lt;=End)
        /// </summary>
        /// <param name="endpoint">The endpoint to move.</param>
        /// <param name="unit">The textual unit for moving.</param>
        /// <param name="count">The number of units to move.  A positive count moves the endpoint forward.  
        /// A negative count moves backward. A count of 0 has no effect.</param>
        /// <returns>The number of units actually moved, which can be less than the number requested if 
        /// moving the endpoint runs into the beginning or end of the document.</returns>
        public int MoveEndpointByUnit(TextPatternRangeEndpoint endpoint, TextUnit unit, int count)
        {
            ValidateEndpointArgument(endpoint, "endpoint");
            ValidateUnitArgument(unit, "unit");

            return UiaCoreApi.TextRange_MoveEndpointByUnit(_hTextRange, endpoint, unit, count);
        }

        /// <summary>
        /// Moves an endpoint of this range to coincide with the endpoint of another range.
        /// </summary>
        /// <param name="endpoint">The endpoint to move.</param>
        /// <param name="targetRange">Another range from the same text provider.</param>
        /// <param name="targetEndpoint">An endpoint on the other range.</param>
        public void MoveEndpointByRange(TextPatternRangeEndpoint endpoint, TextPatternRange targetRange, TextPatternRangeEndpoint targetEndpoint)
        {
            ValidateEndpointArgument(endpoint, "endpoint");
            ValidateRangeArgument(targetRange, "targetRange");
            ValidateEndpointArgument(targetEndpoint, "targetEndpoint");

            UiaCoreApi.TextRange_MoveEndpointByRange(_hTextRange, endpoint, targetRange._hTextRange, targetEndpoint);
        }

        /// <summary>
        /// Selects the text of the range within the provider.
        /// </summary>
        public void Select()
        {
            UiaCoreApi.TextRange_Select(_hTextRange);
        }

        /// <summary>
        /// Adds the text range to the current selection.
        /// </summary>
        public void AddToSelection()
        {
            UiaCoreApi.TextRange_AddToSelection(_hTextRange);
        }

        /// <summary>
        /// Removes the text range from the current selection.
        /// </summary>
        public void RemoveFromSelection()
        {
            UiaCoreApi.TextRange_RemoveFromSelection(_hTextRange);
        }

        /// <summary>
        /// Scrolls the text in the provider so the range is within the viewport.
        /// </summary>
        /// <param name="alignToTop">true if the provider should be scrolled so the range is flush with the top of the viewport.
        /// false if the provider should be scrolled so the range is flush with the bottom.</param>
        public void ScrollIntoView(bool alignToTop)
        {
            UiaCoreApi.TextRange_ScrollIntoView(_hTextRange, alignToTop);
        }

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------
 
        #region Public Properties

        /// <summary>
        /// Retrieves a collection of all of the children that fall within the 
        /// range.
        /// </summary>
        /// <returns>A collection of all children that fall within the range.  Children
        /// that overlap with the range but are not entirely enclosed by it will
        /// also be included in the collection.</returns>
        public AutomationElement[] GetChildren()
        {
            object[] rawChildren = UiaCoreApi.TextRange_GetChildren(_hTextRange);
            AutomationElement[] wrappedChildren = new AutomationElement[rawChildren.Length];
            for (int i = 0; i < rawChildren.Length; i++)
            {
                SafeNodeHandle hnode = UiaCoreApi.UiaHUiaNodeFromVariant(rawChildren[i]);
                wrappedChildren[i] = AutomationElement.Wrap(hnode);
            }
            return wrappedChildren;
        }

        /// <summary>
        /// Retrieves the text provider associated with this range.
        /// </summary>
        /// <returns>The text provider.</returns>
        public TextPattern TextPattern 
        { 
            get
            {
                return _pattern;
            }
        }

        #endregion Public Properties
        
        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
 
        #region Private Methods

        // check an endpoint argument to see if it is valid.
        void ValidateEndpointArgument(TextPatternRangeEndpoint endpoint, string name)
        {
            if (endpoint != TextPatternRangeEndpoint.Start && endpoint != TextPatternRangeEndpoint.End)
            {
                Misc.ThrowInvalidArgument(name);
            }
        }

        // check a range argument to see if it is valid.
        void ValidateRangeArgument(TextPatternRange range, string name)
        {
            // check if the argument is null
            if (range == null)
            {
                throw new ArgumentNullException(name);
            }

            // check if the range comes from a different text pattern.
            if (!TextPattern.Compare(_pattern, range._pattern))
            {
                Misc.ThrowInvalidArgument(name);
            }
}

        // check an unit argument to see if it is valid.
        void ValidateUnitArgument(TextUnit unit, string name)
        {
            if (unit<TextUnit.Character || unit>TextUnit.Document)
            {
                Misc.ThrowInvalidArgument(name);
            }
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields

        SafeTextRangeHandle _hTextRange;
        TextPattern _pattern;

        #endregion Private Fields
    }
}
