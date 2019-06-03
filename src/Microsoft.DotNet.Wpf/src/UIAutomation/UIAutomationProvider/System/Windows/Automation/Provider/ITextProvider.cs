// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Interface exposes a control's ability to manipulate text
//
//

using System;
using System.Windows.Automation;
using System.Runtime.InteropServices;
using System.Windows.Automation.Text;

namespace System.Windows.Automation.Provider
{
    /// <summary>
    /// Represents a text provider that supports the text pattern. 
    /// </summary>
    [ComVisible(true)]
    [Guid("3589c92c-63f3-4367-99bb-ada653b77cf2")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#if (NO_INTERNAL_COMPILE_BUG1080665)
    internal interface ITextProvider
#else
    public interface ITextProvider
#endif
    {
        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------
 
        #region Public Methods

        /// <summary>
        /// Retrieves the current selection.  For providers that have the concept of
        /// text selection the provider should implement this method and also return
        /// true for the SupportsTextSelection property below.  Otherwise this method
        /// should throw an InvalidOperation exception.
        /// For providers that support multiple disjoint selection, this should return
        /// an array of all the currently selected ranges. Providers that don't support
        /// multiple disjoint selection should just return an array containing a single
        /// range.
        /// </summary>
        /// <returns>The range of text that is selected, or possibly null if there is
        /// no selection.</returns>
        ITextRangeProvider [] GetSelection();

        /// <summary>
        /// Retrieves the visible ranges of text.
        /// </summary>
        /// <returns>The ranges of text that are visible, or possibly an empty array if there is
        /// no visible text whatsoever.  Text in the range may still be obscured by an overlapping
        /// window.  Also, portions
        /// of the range at the beginning, in the middle, or at the end may not be visible
        /// because they are scrolled off to the side.
        /// Providers should ensure they return at most a range from the beginning of the first
        /// line with portions visible through the end of the last line with portions visible.</returns>
        ITextRangeProvider [] GetVisibleRanges();

        /// <summary>
        /// Retrieves the range of a child object.
        /// </summary>
        /// <param name="childElement">The child element.  A provider should check that the 
        /// passed element is a child of the text container, and should throw an 
        /// InvalidOperationException if it is not.</param>
        /// <returns>A range that spans the child element.</returns>
        ITextRangeProvider RangeFromChild(IRawElementProviderSimple childElement);

        /// <summary>
        /// Finds the degenerate range nearest to a screen coordinate.
        /// </summary>
        /// <param name="screenLocation">The location in screen coordinates.
        /// The provider should check that the coordinates are within the client
        /// area of the provider, and should throw an InvalidOperation exception 
        /// if they are not.</param>
        /// <returns>A degenerate range nearest the specified location.</returns>
        ITextRangeProvider RangeFromPoint(Point screenLocation);

        #endregion Public Methods
        

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------
 
        #region Public Properties

        /// <summary>
        /// A text range that encloses the main text of the document.  Some auxillary text such as 
        /// headers, footnotes, or annotations may not be included. 
        /// </summary>
        ITextRangeProvider DocumentRange { get; }

        /// <summary>
        /// True if the text container supports text selection. If the provider returns false then
        /// it should throw InvalidOperation exceptions for ITextProvider.GetSelection and 
        /// ITextRangeProvider.Select.
        /// </summary>
        SupportedTextSelection SupportedTextSelection { get; }

        #endregion Public Properties
    }
}


