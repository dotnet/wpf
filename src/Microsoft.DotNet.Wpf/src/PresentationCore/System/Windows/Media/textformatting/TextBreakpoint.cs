// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
//  Contents:  A specialized text line representing state of line up to the 
//             point where line break may occur
//
//  Spec:      Text Formatting API.doc
//
//


using System;
using System.Security;

using MS.Internal;
using MS.Internal.PresentationCore;
using MS.Internal.TextFormatting;


namespace System.Windows.Media.TextFormatting
{
    /// <summary>
    /// A specialized text line representing state of line up to the point
    /// where line break may occur. Unlike the normal text line, breakpoint
    /// could not draw or performs hit-testing operation. It could only reflect
    /// formatting result back to the client.
    /// </summary>
#if OPTIMALBREAK_API
    public abstract class TextBreakpoint : ITextMetrics, IDisposable
#else
    [FriendAccessAllowed]   // used by Framework
    internal abstract class TextBreakpoint : ITextMetrics, IDisposable
#endif
    {
        /// <summary>
        /// Clean up text breakpoint internal resource
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


        /// <summary>
        /// Actual clean up code to be overridden by subclasses, by default it does nothing.
        /// </summary>
        /// <param name="disposing">flag indicates whether the clean up is thru disposal</param>
        protected virtual void Dispose(bool disposing)
        {}



        /// <summary>
        /// Client to acquire a state at the point where breakpoint is determined by line breaking process; 
        /// can be null when the line ends by the ending of the paragraph. Client may pass this
        /// value back to TextFormatter as an input argument to TextFormatter.FormatParagraphBreakpoints when 
        /// formatting the next set of breakpoints within the same paragraph.
        /// </summary>
        public abstract TextLineBreak GetTextLineBreak();


        /// <summary>
        /// Client to get the handle of the internal factors that are used to determine penalty of this breakpoint.
        /// </summary>
        /// <remarks>
        /// Calling this method means that the client will now manage the lifetime of this unmanaged resource themselves using unsafe penalty handler.
        /// We would make a correspondent call to notify our unmanaged wrapper to release them from duty of managing this 
        /// resource. 
        /// </remarks>
        internal abstract SecurityCriticalDataForSet<IntPtr> GetTextPenaltyResource();


        /// <summary>
        /// Client to get a Boolean flag indicating whether the line is truncated in the 
        /// middle of a word. This flag is set only when TextParagraphProperties.TextWrapping 
        /// is set to TextWrapping.Wrap and a single word is longer than the formatting 
        /// paragraph width. In such situation, TextFormatter truncates the line in the middle 
        /// of the word to honor the desired behavior specified by TextWrapping.Wrap setting.
        /// </summary>
        public abstract bool IsTruncated 
        { get; }


        #region ITextMetrics

        /// <summary>
        /// Client to get the number of text source positions of this line
        /// </summary>
        public abstract int Length 
        { get; }


        /// <summary>
        /// Client to get the number of characters following the last character 
        /// of the line that may trigger reformatting of the current line.
        /// </summary>
        public abstract int DependentLength 
        { get; }


        /// <summary>
        /// Client to get the number of newline characters at line end
        /// </summary>
        public abstract int NewlineLength 
        { get; }


        /// <summary>
        /// Client to get distance from paragraph start to line start
        /// </summary>
        public abstract double Start 
        { get; }


        /// <summary>
        /// Client to get the total width of this line
        /// </summary>
        public abstract double Width 
        { get; }


        /// <summary>
        /// Client to get the total width of this line including width of whitespace characters at the end of the line.
        /// </summary>
        public abstract double WidthIncludingTrailingWhitespace 
        { get; }


        /// <summary>
        /// Client to get the height of the line
        /// </summary>
        public abstract double Height 
        { get; }


        /// <summary>
        /// Client to get the height of the text (or other content) in the line; this property may differ from the Height
        /// property if the client specified the line height
        /// </summary>
        public abstract double TextHeight
        { get; }


        /// <summary>
        /// Client to get the distance from top to baseline of this text line
        /// </summary>
        public abstract double Baseline 
        { get; }


        /// <summary>
        /// Client to get the distance from the top of the text (or other content) to the baseline of this text line;
        /// this property may differ from the Baseline property if the client specified the line height
        /// </summary>
        public abstract double TextBaseline
        { get; }


        /// <summary>
        /// Client to get the distance from the before edge of line height 
        /// to the baseline of marker of the line if any.
        /// </summary>
        public abstract double MarkerBaseline 
        { get; }


        /// <summary>
        /// Client to get the overall height of the list items marker of the line if any.
        /// </summary>
        public abstract double MarkerHeight 
        { get; }

        #endregion
    }
}

