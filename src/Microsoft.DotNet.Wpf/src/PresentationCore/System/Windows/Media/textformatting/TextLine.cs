// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
//  Contents:  Text line API
//
//  Spec:      Text Formatting API.doc
//
//


using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using MS.Internal.TextFormatting;
using MS.Internal.PresentationCore;


namespace System.Windows.Media.TextFormatting
{
    /// <summary>
    /// Once a line is complete, FormatLine returns a TextLine object to the client. 
    /// The text layout client may then use the TextLines measurement and drawing methods.
    /// </summary>
    public abstract class TextLine : ITextMetrics, IDisposable
    {
        /// <summary>
        /// Constructing a TextLine. This constructor must be called for every class that is inherited from TextLine
        /// </summary>
        /// <param name="pixelsPerDip">pixelsPerDip should be set to the TextSource's PixelsPerDip</param>
        protected TextLine(double pixelsPerDip)
        {
            _pixelsPerDip = pixelsPerDip;
        }

        protected TextLine()
        {
        }

        /// <summary>
        /// Clean up text line internal resource
        /// </summary>
        public abstract void Dispose();


        /// <summary>
        /// Client to draw the line
        /// </summary>
        /// <param name="drawingContext">drawing context</param>
        /// <param name="origin">drawing origin</param>
        /// <param name="inversion">indicate the inversion of the drawing surface</param>
        public abstract void Draw(
            DrawingContext      drawingContext,
            Point               origin,
            InvertAxes          inversion
            );


        /// <summary>
        /// Client to collapse the line and get a collapsed line that fits for display
        /// </summary>
        /// <param name="collapsingPropertiesList">a list of collapsing properties</param>
        public abstract TextLine Collapse(
            params TextCollapsingProperties[]   collapsingPropertiesList
            );


        /// <summary>
        /// Client to get a collection of collapsed character ranges after a line has been collapsed
        /// </summary>
        public abstract IList<TextCollapsedRange> GetTextCollapsedRanges();


        /// <summary>
        /// Client to get the character hit corresponding to the specified 
        /// distance from the beginning of the line.
        /// </summary>
        /// <param name="distance">distance in text flow direction from the beginning of the line</param>
        /// <returns>character hit</returns>
        public abstract CharacterHit GetCharacterHitFromDistance(
            double      distance
            );

        
        /// <summary>
        /// Client to get the distance from the beginning of the line from the specified 
        /// character hit.
        /// </summary>
        /// <param name="characterHit">character hit of the character to query the distance.</param>
        /// <returns>distance in text flow direction from the beginning of the line.</returns>
        public abstract double GetDistanceFromCharacterHit(
            CharacterHit    characterHit
            );


        /// <summary>
        /// Client to get the next character hit for caret navigation
        /// </summary>
        /// <param name="characterHit">the current character hit</param>
        /// <returns>the next character hit</returns>
        public abstract CharacterHit GetNextCaretCharacterHit(
            CharacterHit    characterHit
            );


        /// <summary>
        /// Client to get the previous character hit for caret navigation
        /// </summary>
        /// <param name="characterHit">the current character hit</param>
        /// <returns>the previous character hit</returns>
        public abstract CharacterHit GetPreviousCaretCharacterHit(
            CharacterHit    characterHit
            );


        /// <summary>
        /// Client to get the previous character hit after backspacing
        /// </summary>
        /// <param name="characterHit">the current character hit</param>
        /// <returns>the character hit after backspacing</returns>
        public abstract CharacterHit GetBackspaceCaretCharacterHit(
            CharacterHit    characterHit
            );

        public double PixelsPerDip
        {
            get { return _pixelsPerDip; }
            set { _pixelsPerDip = value; }
        }

        private double _pixelsPerDip = MS.Internal.FontCache.Util.PixelsPerDip;

        /// <summary>
        /// Determine whether the input character hit is a valid caret stop. 
        /// </summary>
        /// <param name="characterHit">the current character hit</param>
        /// <param name="cpFirst">the starting cp index of the line</param>
        /// <remarks>
        /// It is used by Framework to iterate through each codepoint in the line to 
        /// see if the code point is a caret stop. In general, the leading edge of 
        /// codepoint is a valid caret stop if moving forward and then backward will 
        /// return back to it,  vice versa for the trailing edge of a codepoint. 
        /// </remarks>
        [FriendAccessAllowed]
        internal bool IsAtCaretCharacterHit(CharacterHit characterHit, int cpFirst)
        {   
            // TrailingLength is used as a flag to indicate whether the character 
            // hit is on the leading or trailing edge of the character.
            if (characterHit.TrailingLength == 0)
            {
                CharacterHit nextHit = GetNextCaretCharacterHit(characterHit);
                if (nextHit == characterHit)
                {
                    // At this point we only know that no caret stop is available 
                    // after the input, we caliberate the input to the end of the line.
                    nextHit = new CharacterHit(cpFirst + Length - 1, 1);
                }

                CharacterHit previousHit = GetPreviousCaretCharacterHit(nextHit);
                return previousHit == characterHit;
            }
            else
            {
                CharacterHit previousHit = GetPreviousCaretCharacterHit(characterHit);              
                CharacterHit nextHit     = GetNextCaretCharacterHit(previousHit);
                return nextHit == characterHit; 
            }
        }
        


        /// <summary>
        /// Client to get an array of bounding rectangles of a range of characters within a text line.
        /// </summary>
        /// <param name="firstTextSourceCharacterIndex">index of first character of specified range</param>
        /// <param name="textLength">number of characters of the specified range</param>
        /// <returns>an array of bounding rectangles.</returns>
        public abstract IList<TextBounds> GetTextBounds(
            int     firstTextSourceCharacterIndex,
            int     textLength
            );


        /// <summary>
        /// Client to get a collection of TextRun span objects within a line
        /// </summary>
        public abstract IList<TextSpan<TextRun>> GetTextRunSpans();


        /// <summary>
        /// Client to get IndexedGlyphRuns enumerable to enumerate each IndexedGlyphRun object 
        /// in the line. Through IndexedGlyphRun client can obtain glyph information of 
        /// a text source character. 
        /// </summary>
        public abstract IEnumerable<IndexedGlyphRun> GetIndexedGlyphRuns();        


        /// <summary>
        /// Client to get a boolean value indicates whether content of the line overflows 
        /// the specified paragraph width.
        /// </summary>
        public abstract bool HasOverflowed 
        { get; }


        /// <summary>
        /// Client to get a boolean value indicates whether a line has been collapsed
        /// </summary>
        public abstract bool HasCollapsed 
        { get; }


        /// <summary>
        /// Client to get a Boolean flag indicating whether the line is truncated in the 
        /// middle of a word. This flag is set only when TextParagraphProperties.TextWrapping 
        /// is set to TextWrapping.Wrap and a single word is longer than the formatting 
        /// paragraph width. In such situation, TextFormatter truncates the line in the middle 
        /// of the word to honor the desired behavior specified by TextWrapping.Wrap setting.
        /// </summary>
        public virtual bool IsTruncated
        {
            get { return false; }
        }


        /// <summary>
        /// Client to acquire a state at the point where line is broken by line breaking process; 
        /// can be null when the line ends by the ending of the paragraph. Client may pass this 
        /// value back to TextFormatter as an input argument to TextFormatter.FormatLine when 
        /// formatting the next line within the same paragraph.
        /// </summary>
        public abstract TextLineBreak GetTextLineBreak();

        #region ITextMetrics

        /// <summary>
        /// Client to get the number of text source positions of this line
        /// </summary>
        public abstract int Length 
        { get; }


        /// <summary>
        /// Client to get the number of whitespace characters at the end of the line.
        /// </summary>
        public abstract int TrailingWhitespaceLength 
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
        /// Client to get the height of the actual black of the line
        /// </summary>
        public abstract double Extent 
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


        /// <summary>
        /// Client to get the distance covering all black preceding the leading edge of the line.
        /// </summary>
        public abstract double OverhangLeading 
        { get; }


        /// <summary>
        /// Client to get the distance covering all black following the trailing edge of the line.
        /// </summary>
        public abstract double OverhangTrailing 
        { get; }


        /// <summary>
        /// Client to get the distance from the after edge of line height to the after edge of the extent of the line.
        /// </summary>
        public abstract double OverhangAfter 
        { get; }       
        #endregion
    }


    /// <summary>
    /// Indicate the inversion of axes of the drawing surface
    /// </summary>
    [Flags]
    public enum InvertAxes
    {
        /// <summary>
        /// Drawing surface is not inverted in either axis
        /// </summary>
        None = 0,

        /// <summary>
        /// Drawing surface is inverted in horizontal axis
        /// </summary>
        Horizontal = 1,

        /// <summary>
        /// Drawing surface is inverted in vertical axis
        /// </summary>
        Vertical = 2,

        /// <summary>
        /// Drawing surface is inverted in both axes
        /// </summary>
        Both = (Horizontal | Vertical),
    }
}
