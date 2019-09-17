// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Interface exposes a control's ability to manipulate text ranges

using System;
using System.Collections;
using System.Windows.Automation;
using System.Windows.Automation.Text;
using System.Runtime.InteropServices;

namespace System.Windows.Automation.Provider
{
    /// <summary>
    /// Represents a span of text in a text provider. 
    /// </summary>
    [ComVisible(true)]
    [Guid("5347ad7b-c355-46f8-aff5-909033582f63")]  // review: an official guid source?
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#if (NO_INTERNAL_COMPILE_BUG1080665)
    internal interface ITextRangeProvider
#else
    public interface ITextRangeProvider
#endif
    {
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
        ITextRangeProvider Clone();

        /// <summary>
        /// Compares this range with another range.
        /// </summary>
        /// <param name="range">A range to compare. 
        /// The range must have come from the same text provider or an InvalidArgumentException will be thrown.</param>
        /// <returns>true if both ranges span the same text.</returns>
        [return: MarshalAs(UnmanagedType.Bool)]
        bool Compare(ITextRangeProvider range);

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
        int CompareEndpoints(TextPatternRangeEndpoint endpoint, ITextRangeProvider targetRange, TextPatternRangeEndpoint targetEndpoint);

        /// <summary>
        /// Expands the range to an integral number of enclosing units.  this could be used, for example,
        /// to guarantee that a range endpoint is not in the middle of a word.  If the range is already an
        /// integral number of the specified units then it remains unchanged.
        /// </summary>
        /// <param name="unit">The textual unit.</param>
        void ExpandToEnclosingUnit(TextUnit unit);

        /// <summary>
        /// Searches for a subrange of text that has the specified attribute.  To search the entire document use the text provider's
        /// document range.
        /// </summary>
        /// <param name="attribute">The attribute to search for.</param>
        /// <param name="value">The value of the specified attribute to search for.</param>
        /// <param name="backward">true if the last occurring range should be returned instead of the first.</param>
        /// <returns>A subrange with the specified attribute, or null if no such subrange exists.</returns>
        ITextRangeProvider FindAttribute(int attribute, object value, [MarshalAs(UnmanagedType.Bool)] bool backward);

        /// <summary>
        /// Searches for an occurrence of text within the range.
        /// </summary>
        /// <param name="text">The text to search for.</param>
        /// <param name="backward">true if the last occurring range should be returned instead of the first.</param>
        /// <param name="ignoreCase">true if case should be ignored for the purposes of comparison.</param>
        /// <returns>A subrange with the specified text, or null if no such subrange exists.</returns>
        ITextRangeProvider FindText(string text, [MarshalAs(UnmanagedType.Bool)] bool backward, [MarshalAs(UnmanagedType.Bool)] bool ignoreCase);

        /// <summary>
        /// Retrieves the value of a text attribute over the entire range.
        /// </summary>
        /// <param name="attribute">The text attribute.</param>
        /// <returns>The value of the attribute across the range. 
        /// If the attribute's value varies over the range then the value is TextPattern.MixedAttributeValue</returns>
        object GetAttributeValue(int attribute);

        /// <summary>
        /// Retrieves the bounding rectangles for viewable lines of the range.
        /// </summary>
        /// <returns>An array of bounding rectangles for each line or portion of a line within the client area of the text provider.
        /// No bounding rectangles will be returned for lines that are empty or scrolled out of view.  Note that even though a
        /// bounding rectangle is returned the corresponding text may not be visible due to overlapping windows.
        /// This will not return null, but may return an empty array.
        /// 
        /// 
        /// </returns>
        double [] GetBoundingRectangles();

        /// <summary>
        /// Retrieves the innermost element that encloses this range.
        /// </summary>
        /// <returns>An element.  Usually this element will be the one that supplied this range.
        /// However, if the text provider supports child elements such as tables or hyperlinks, then the
        /// enclosing element could be a descendant element of the text provider.
        /// </returns>
        IRawElementProviderSimple GetEnclosingElement();

        /// <summary>
        /// Retrieves the text of the range.
        /// </summary>
        /// <param name="maxLength">Specifies the maximum length of the string to return or -1 if no limit is requested.</param>
        /// <returns>The text of the range possibly truncated to the specified limit.</returns>
        string GetText(int maxLength);

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
        int Move(TextUnit unit, int count);

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
        int MoveEndpointByUnit(TextPatternRangeEndpoint endpoint, TextUnit unit, int count);

        /// <summary>
        /// Moves an endpoint of this range to coincide with the endpoint of another range.
        /// </summary>
        /// <param name="endpoint">The endpoint to move.</param>
        /// <param name="targetRange">Another range from the same text provider.</param>
        /// <param name="targetEndpoint">An endpoint on the other range.</param>
        void MoveEndpointByRange(TextPatternRangeEndpoint endpoint, ITextRangeProvider targetRange, TextPatternRangeEndpoint targetEndpoint);

        /// <summary>
        /// Selects the text of the range within the provider.  If the provider does not have a concept of selection then
        /// it should return false for ITextProvider.SupportsTextSelection property and throw an InvalidOperation 
        /// exception for this method.
        /// </summary>
        void Select();

        /// <summary>
        /// Adds the text of the range to the current selection.  If the provider does not have a concept of selection
        /// or does not support multiple disjoint selection then it throw an InvalidOperation 
        /// exception for this method.
        /// </summary>
        void AddToSelection();

        /// <summary>
        /// Removes the text of the range from the current selection.  If the provider does not have a concept of selection
        /// or does not support multiple disjoint selection then it throw an InvalidOperation 
        /// exception for this method.
        /// </summary>
        void RemoveFromSelection();

        /// <summary>
        /// Scrolls the text in the provider so the range is within the viewport.
        /// </summary>
        /// <param name="alignToTop">true if the provider should be scrolled so the range is flush with the top of the viewport.
        /// false if the provider should be scrolled so the range is flush with the bottom.</param>
        void ScrollIntoView([MarshalAs(UnmanagedType.Bool)] bool alignToTop);
     
        #endregion Public Methods

        
        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------
 
        #region Public Properties

        /// <summary>
        /// Retrieves a collection of all of the children that fall within the range.
        /// </summary>
        /// <returns>An enumeration of all children that fall within the range.  Children
        /// that overlap with the range but are not entirely enclosed by it will
        /// also be included in the collection.  If there are no children then
        /// this can return either null or an empty enumeration.</returns>
        IRawElementProviderSimple[] GetChildren();

        #endregion Public Properties
    }
}
