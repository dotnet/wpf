// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: 
//   Base class for TextRange find functionality.
//
//   See spec at: Find Spec.doc
//

using MS.Internal;
using MS.Utility;
using MS.Win32;

using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Security;
using System.Text;
using System.Windows;

namespace System.Windows.Documents
{
    /// <summary>
    /// Find options.
    /// </summary>
    [Flags]
    internal enum FindFlags
    {
        /// <summary>
        /// None.
        /// </summary>
        None                = 0x00000000,
        /// <summary>
        /// Match case.
        /// </summary>
        MatchCase           = 0x00000001,
        /// <summary>
        /// Searches for last occurance.
        /// </summary>
        FindInReverse       = 0x00000002,
        /// <summary>
        /// Matches the entire word.
        /// </summary>
        FindWholeWordsOnly  = 0x00000004,
        /// <summary>
        /// Matches Bidi diacritics.
        /// </summary>
        MatchDiacritics     = 0x00000008,
        /// <summary>
        /// Matches Arabic kashida.
        /// </summary>
        MatchKashida        = 0x00000010,
        /// <summary>
        /// Matches Arabic AlefHamza.
        /// </summary>
        MatchAlefHamza      = 0x00000020,
    }


    /// <summary>
    /// Internal access point for find engines.
    /// </summary>
    internal static class TextFindEngine
    {
        //---------------------------------------------------------------------
        //
        // Public methods
        //
        //---------------------------------------------------------------------

        #region Public methods

        /// <summary>
        /// Performs find operation on of a given text range.
        /// </summary>
        /// <param name="findContainerStartPosition">Text position to start search.</param>
        /// <param name="findContainerEndPosition">Text position to end search.</param>
        /// <param name="findPattern">Pattern to find.</param>
        /// <param name="flags">Find flags.</param>
        /// <param name="cultureInfo">Culture specific information.</param>
        /// <returns>TextRange for the result or <c>null</c></returns>
        /// <remarks>
        /// Very limited functionality for now
        /// </remarks>
        public static ITextRange Find(
            ITextPointer findContainerStartPosition,
            ITextPointer findContainerEndPosition,
            string findPattern,
            FindFlags flags,
            CultureInfo cultureInfo)
        {
            //  throw exceptions here
            if (findContainerStartPosition == null
                || findContainerEndPosition == null
                || findContainerStartPosition.CompareTo(findContainerEndPosition) == 0
                || findPattern == null
                || findPattern == string.Empty )
            {
                return (null);
            }

            TextRange findResult = null;
            bool matchCase = (flags & FindFlags.MatchCase) != 0;
            bool matchWholeWord = (flags & FindFlags.FindWholeWordsOnly) != 0;
            bool matchLast = (flags & FindFlags.FindInReverse) != 0;
            bool matchDiacritics = (flags & FindFlags.MatchDiacritics) != 0;
            bool matchKashida = (flags & FindFlags.MatchKashida) != 0;
            bool matchAlefHamza = (flags & FindFlags.MatchAlefHamza) != 0;

            if (matchWholeWord)
            {
                UInt16[] findPatternStartCharType1 = new UInt16[1];
                UInt16[] findPatternEndCharType1 = new UInt16[1];
                char[] charFindPattern = findPattern.ToCharArray();

                // Get the character type for the start/end character of the find pattern.
                SafeNativeMethods.GetStringTypeEx(0 /* ignored */, SafeNativeMethods.CT_CTYPE1, new char[] { charFindPattern[0] }, 1, findPatternStartCharType1);
                SafeNativeMethods.GetStringTypeEx(0 /* ignored */, SafeNativeMethods.CT_CTYPE1, new char[] { charFindPattern[findPattern.Length - 1] }, 1, findPatternEndCharType1);

                // Reset the finding whole word flag if FindPattern includes the space
                // or blank character at the start or end position.
                if ((findPatternStartCharType1[0] & SafeNativeMethods.C1_SPACE) != 0 ||
                    (findPatternStartCharType1[0] & SafeNativeMethods.C1_BLANK) != 0 ||
                    (findPatternEndCharType1[0] & SafeNativeMethods.C1_SPACE) != 0 ||
                    (findPatternEndCharType1[0] & SafeNativeMethods.C1_BLANK) != 0)
                {
                    matchWholeWord = false;
                }
            }

            //If this we're searching on a Fixed layout, we need to do a faster search that takes into accout
            //page-per-stream scenarios
            if (findContainerStartPosition is DocumentSequenceTextPointer ||
                findContainerStartPosition is FixedTextPointer)
            {
                return FixedFindEngine.Find(findContainerStartPosition, 
                                            findContainerEndPosition,
                                            findPattern,
                                            cultureInfo,
                                            matchCase, 
                                            matchWholeWord,
                                            matchLast,
                                            matchDiacritics,
                                            matchKashida,
                                            matchAlefHamza);
            }
            
            // Find the text with the specified option flags.
            findResult = InternalFind(
                findContainerStartPosition,
                findContainerEndPosition,
                findPattern,
                cultureInfo,
                matchCase,
                matchWholeWord,
                matchLast,
                matchDiacritics,
                matchKashida,
                matchAlefHamza);

            return (findResult);
        }

        #endregion Public methods

        //---------------------------------------------------------------------
        //
        // Private methods
        //
        //---------------------------------------------------------------------

        #region Private Methods

        // Find the text with the specified find options.
        internal static TextRange InternalFind(
            ITextPointer startPosition,
            ITextPointer endPosition,
            string findPattern,
            CultureInfo cultureInfo,
            bool matchCase,
            bool matchWholeWord,
            bool matchLast,
            bool matchDiacritics,
            bool matchKashida,
            bool matchAlefHamza)
        {
            Invariant.Assert(startPosition.CompareTo(endPosition) <= 0);

            ITextPointer navigator;
            LogicalDirection direction;

            char[] findText;
            int[] findTextPositionMap;
            int findTextLength;

            if (matchLast)
            {
                navigator = endPosition;
                direction = LogicalDirection.Backward;
            }
            else
            {
                navigator = startPosition;
                direction = LogicalDirection.Forward;
            }
            
            // Set the text block size to read the find text content.
            // The block size must be bigger than the double of find pattern size
            // so that we can find matches intersected by neighboring blocks.
            // We need an additional x2 fudge factor to account for the matchDiacritics
            // option -- the findPattern may lack diacritics that will be ignored in
            // the text.
            int textBlockLength = Math.Max(TextBlockLength, findPattern.Length * 2 * 2);

            navigator = navigator.CreatePointer();

            while ((matchLast ? startPosition.CompareTo(navigator) : navigator.CompareTo(endPosition)) < 0)
            {
                ITextPointer startFindTextPosition = navigator.CreatePointer();

                findText = new char[textBlockLength];
                findTextPositionMap = new int[textBlockLength + 1];

                // Set the find text content from reading text of the current text position.
                // Set the find text position map as well to track of the text pointer of the text content.
                findTextLength = SetFindTextAndFindTextPositionMap(
                    startPosition, 
                    endPosition, 
                    navigator, 
                    direction,
                    matchLast,
                    findText,
                    findTextPositionMap);

                if (!matchDiacritics || findTextLength >= findPattern.Length)
                {
                    int textStartIndex = matchLast ? findText.Length - findTextLength : 0;

                    // Track whether or not the text array findText is bounded by
                    // separator chars.  We only look at these values when matchWholeWord
                    // is true.
                    bool hasPreceedingSeparatorChar = false;
                    bool hasFollowingSeparatorChar = false;

                    if (matchWholeWord)
                    {
                        GetContextualInformation(startFindTextPosition, matchLast ? -findTextPositionMap[findTextPositionMap.Length - findTextLength - 1] : findTextPositionMap[findTextLength],
                                                 out hasPreceedingSeparatorChar, out hasFollowingSeparatorChar);
                    }

                    string textString = new string(findText, textStartIndex, findTextLength);
                    int matchLength;

                    // Now find text the matched index for the find pattern from the find text content
                    int matchIndex = FindMatchIndexFromFindContent(
                        textString,
                        findPattern,
                        cultureInfo,
                        matchCase,
                        matchWholeWord,
                        matchLast,
                        matchDiacritics,
                        matchKashida,
                        matchAlefHamza,
                        hasPreceedingSeparatorChar,
                        hasFollowingSeparatorChar,
                        out matchLength);

                    if (matchIndex != -1)
                    {
                        // Found the find pattern string from the find text content!
                        // Return the text range for the matched find text position.

                        ITextPointer startMatchPosition = startFindTextPosition.CreatePointer();
                        startMatchPosition.MoveByOffset(matchLast ? - findTextPositionMap[textStartIndex + matchIndex] : findTextPositionMap[matchIndex]);

                        ITextPointer endMatchPosition = startFindTextPosition.CreatePointer();
                        endMatchPosition.MoveByOffset(matchLast ? - findTextPositionMap[textStartIndex + matchIndex + matchLength] : findTextPositionMap[matchIndex + matchLength]);

                        return (new TextRange(startMatchPosition, endMatchPosition));
                    }

                    // Move the text position for the size of finding pattern string not to miss
                    // the matching text that is located on the boundary of the find text block.
                    // Move back the position to N(findTextLength) - findPattern.Length
                    if (findTextLength > findPattern.Length)
                    {
                        // Need to set new pointer to jump the correct place of backing offset of the findPattern length
                        navigator = startFindTextPosition.CreatePointer();
                        navigator.MoveByOffset(matchLast ? - findTextPositionMap[findText.Length - findTextLength + findPattern.Length] : findTextPositionMap[findTextLength - findPattern.Length]);
                    }
                }
            }

            return (null);
        }

        // Returns information about the text immediately preceeding and following a
        // "window" specified by a position at one end and an offset to the other end.
        // If oppositeEndOffset is positive, then position is at the left edge of the
        // window; otherwise it is at the right edge.
        private static void GetContextualInformation(ITextPointer position, int oppositeEndOffset,
            out bool hasPreceedingSeparatorChar, out bool hasFollowingSeparatorChar)
        {
            ITextPointer oppositeEndPosition = position.CreatePointer(oppositeEndOffset, position.LogicalDirection);

            if (oppositeEndOffset < 0)
            {
                hasPreceedingSeparatorChar = HasNeighboringSeparatorChar(oppositeEndPosition, LogicalDirection.Backward);
                hasFollowingSeparatorChar = HasNeighboringSeparatorChar(position, LogicalDirection.Forward);
            }
            else
            {
                hasPreceedingSeparatorChar = HasNeighboringSeparatorChar(position, LogicalDirection.Backward);
                hasFollowingSeparatorChar = HasNeighboringSeparatorChar(oppositeEndPosition, LogicalDirection.Forward);
            }
        }

        // Returns true iff there is a character in the specificed direction adjacent to a
        // position which is classified as a separator.  This is useful in detecting word breaks.
        private static bool HasNeighboringSeparatorChar(ITextPointer position, LogicalDirection direction)
        {
            ITextPointer nextPosition = position.GetNextInsertionPosition(direction);

            if (nextPosition == null)
            {
                return true;
            }

            if (position.CompareTo(nextPosition) > 0)
            {
                ITextPointer temp = position;
                position = nextPosition;
                nextPosition = temp;
            }

            int maxCharCount = position.GetOffsetToPosition(nextPosition);
            char[] findText = new char[maxCharCount];
            int []findTextPositionMap = new int[maxCharCount + 1];
            int findTextLength;

            findTextLength = SetFindTextAndFindTextPositionMap(
                                position,
                                nextPosition,
                                position.CreatePointer() /* need unfrozen pointer */,
                                LogicalDirection.Forward,
                                false /* matchLast */,
                                findText,
                                findTextPositionMap);

            if (findTextLength == 0)
            {
                return true;
            }

            bool hasNeighboringSeparatorChar;

            if (direction == LogicalDirection.Forward)
            {
                hasNeighboringSeparatorChar = IsSeparatorChar(findText[0]);
            }
            else
            {
                hasNeighboringSeparatorChar = IsSeparatorChar(findText[findTextLength-1]);
            }

            return hasNeighboringSeparatorChar;
        }
        
        /// <summary>
        /// Find text and return the matched index of the find text content.
        /// </summary>
        private static int FindMatchIndexFromFindContent(
            string textString,
            string findPattern,
            CultureInfo cultureInfo,
            bool matchCase,
            bool matchWholeWord,
            bool matchLast,
            bool matchDiacritics,
            bool matchKashida,
            bool matchAlefHamza,
            bool hasPreceedingSeparatorChar,
            bool hasFollowingSeparatorChar,
            out int matchLength)
        {
            bool stringContainedBidiCharacter;
            bool stringContainedAlefCharacter;

            // Initialize Bidi flags whether the string contains the bidi characters
            // or alef character.
            InitializeBidiFlags(
                findPattern,
                out stringContainedBidiCharacter,
                out stringContainedAlefCharacter);

            CompareInfo compareInfo = cultureInfo.CompareInfo;
            int matchIndex;

            // Ignore Bidi diacritics that use for only Bidi language.
            if (!matchDiacritics && stringContainedBidiCharacter)
            {
                // Ignore Bidi diacritics with checking non-space character.
                matchIndex = BidiIgnoreDiacriticsMatchIndexCalculation(textString, findPattern, matchKashida, matchAlefHamza, matchWholeWord, matchLast, !matchCase, compareInfo, hasPreceedingSeparatorChar, hasFollowingSeparatorChar, out matchLength);
            }
            else
            {
                matchIndex = StandardMatchIndexCalculation(textString, findPattern, matchWholeWord, matchLast, !matchCase, compareInfo, hasPreceedingSeparatorChar, hasFollowingSeparatorChar, out matchLength);
            }

            return matchIndex;
        }

        // Returns the index and length of the first or last occurance of one string
        // within another string.
        private static int StandardMatchIndexCalculation(string textString, string findPattern, bool matchWholeWord, bool matchLast, bool ignoreCase, CompareInfo compareInfo, bool hasPreceedingSeparatorChar, bool hasFollowingSeparatorChar, out int matchLength)
        {
            CompareOptions options = ignoreCase ? CompareOptions.IgnoreCase : 0;
            int matchIndex = -1;
            int searchStart = 0;
            int searchLength = textString.Length;

            matchLength = 0;

            while (searchLength > 0)
            {
                matchIndex = matchLast ?
                             compareInfo.LastIndexOf(textString, findPattern, searchStart + searchLength - 1, searchLength, options) :
                             compareInfo.IndexOf(textString, findPattern, searchStart, searchLength, options);

                matchLength = findPattern.Length;

                if (matchIndex == -1)
                {
                    break;
                }

                if (!matchWholeWord || IsAtWordBoundary(textString, matchIndex, matchLength, hasPreceedingSeparatorChar, hasFollowingSeparatorChar))
                {
                    break;
                }

                if (matchLast)
                {
                    searchStart = 0;
                    searchLength = matchIndex + matchLength - 1;
                }
                else
                {
                    searchStart = matchIndex + 1;
                    searchLength = textString.Length - searchStart;
                }

                matchIndex = -1;
            }

            return matchIndex;
        }

        // Returns the index and length of the first or last occurance of one string
        // within another string.
        //
        // Performs a brute force n^2 pattern match search, necessary when FindFlags.MatchDiacritics == false
        // on bidi content.  (Because textString is not unbounded with document size, performance is not
        // completely broken.)
        //
        // In Vista and later OSs, the native FindNLSString API classifies arabic diacriticals
        // as non-spacing characters and provides IndexOf/LastIndexOf functionality.
        private static int BidiIgnoreDiacriticsMatchIndexCalculation(string textString, string findPattern, bool matchKashida, bool matchAlefHamza, bool matchWholeWord, bool matchLast, bool ignoreCase, CompareInfo compareInfo, bool hasPreceedingSeparatorChar, bool hasFollowingSeparatorChar, out int matchLength)
        {
            // NB: See bug 1629855.  There is a bug in the xp nls tables where CompareOptions.IgnoreNonSpace will
            // not ignore all arabic diacriticals.  We don't have a fix for this problem on xp.  On Vista
            // we work around by using FindNLSString.

            int matchIndex = -1;
            int startIndex = matchLast ? textString.Length - 1 : 0;
            int endIndex = matchLast ? -1 : textString.Length;
            int delta = matchLast ? -1 : +1;

            if (System.Environment.OSVersion.Version.Major >= 6)
            {
                const uint NORM_IGNORECASE = 0x00000001; // ignore case
                const uint NORM_IGNORENONSPACE = 0x00000002; // ignore nonspacing chars
                const uint FIND_FROMEND = 0x00800000; // look for value in source, starting at the end

                // In Vista, the flag meaning is "IGNORE NONSPACE == IGNORE MADDA == IGNOREHAMZA"
                // The flag NORM_IGNORENONSPACE will ignore Bidi diacratics, Hamza and Madda.
                // Kashida is always ignored no matter what flag is set.

                uint findNLSStringFlags = NORM_IGNORENONSPACE;

                if (ignoreCase)
                {
                    findNLSStringFlags |= NORM_IGNORECASE;
                }

                if (matchLast)
                {
                    findNLSStringFlags |= FIND_FROMEND;
                }

                if (matchKashida)
                {
                    // Replace kashida for MatchKashida
                    textString = textString.Replace(UnicodeArabicKashida, '0');
                    findPattern = findPattern.Replace(UnicodeArabicKashida, '0');
                }

                if (matchAlefHamza)
                {
                    // Replace Hamza and Madda for MatchAlefHamza
                    textString = textString.Replace(UnicodeArabicAlefMaddaAbove, '0');
                    textString = textString.Replace(UnicodeArabicAlefHamzaAbove, '1');
                    textString = textString.Replace(UnicodeArabicAlefHamzaBelow, '2');

                    findPattern = findPattern.Replace(UnicodeArabicAlefMaddaAbove, '0');
                    findPattern = findPattern.Replace(UnicodeArabicAlefHamzaAbove, '1');
                    findPattern = findPattern.Replace(UnicodeArabicAlefHamzaBelow, '2');
                }

                matchLength = 0;

                // Find the match index from FindNLSString API which is only in Vista
                if (matchWholeWord)
                {
                    for (int i = startIndex; matchIndex == -1 && i != endIndex; i += delta)
                    {
                        for (int j = i; j < textString.Length; j++)
                        {
                            string subString = textString.Substring(i, j - i + 1);

                            int subStringIndex = FindNLSString(compareInfo.LCID, findNLSStringFlags,
                                                    subString, findPattern, out matchLength);

                            if (subStringIndex >= 0 &&
                                IsAtWordBoundary(textString, i + subStringIndex, matchLength, hasPreceedingSeparatorChar, hasFollowingSeparatorChar))
                            {
                                matchIndex = i + subStringIndex;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    matchIndex = FindNLSString(compareInfo.LCID, findNLSStringFlags,
                                    textString, findPattern, out matchLength);
                }
            }
            else
            {
                CompareOptions options = CompareOptions.IgnoreNonSpace | (ignoreCase ? CompareOptions.IgnoreCase : 0);

                matchLength = 0;

                for (int i = startIndex; matchIndex == -1 && i != endIndex; i += delta)
                {
                    for (int j = i; j < textString.Length; j++)
                    {
                        if (compareInfo.Compare(textString, i, j - i + 1, findPattern, 0, findPattern.Length, options) == 0 &&
                            (!matchWholeWord || IsAtWordBoundary(textString, i, j - i + 1, hasPreceedingSeparatorChar, hasFollowingSeparatorChar)))
                        {
                            if ((!matchKashida || IsKashidaMatch(textString.Substring(i, j - i + 1), findPattern, compareInfo)) &&
                                (!matchAlefHamza || IsAlefHamzaMatch(textString.Substring(i, j - i + 1), findPattern, compareInfo)))
                            {
                                matchIndex = i;
                                matchLength = j - i + 1;
                                break;
                            }
                        }
                    }
                }
            }

            return matchIndex;
        }

        //  Fixing method signature to meet TAS security requirements.
        private static int FindNLSString(int locale, uint flags, string sourceString, string findString, out int found)
        {
            int matchIndex = UnsafeNativeMethods.FindNLSString(locale, flags,
                                sourceString, sourceString.Length, findString, findString.Length, out found);

            if (matchIndex == -1)
            {
                int win32Error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                if (win32Error != 0)
                {
                    throw new System.ComponentModel.Win32Exception(win32Error);
                }
            }

            return matchIndex;
        }

        // Returns true iff two matching strings continue to match when kashida are considered.
        private static bool IsKashidaMatch(string text, string pattern, CompareInfo compareInfo)
        {
            const CompareOptions options = CompareOptions.IgnoreSymbols | CompareOptions.StringSort | CompareOptions.IgnoreNonSpace;

            // Relace the Kashida char with a non-symbolic, non-diacritic constant value.
            // Kashida is ignored with the CompareOptions we use during the search.
            // When called, it's ok if the constant value matches other chars in the find pattern.
            // We've already got a match ignoring kashida.

            text = text.Replace(UnicodeArabicKashida, '0');
            pattern = pattern.Replace(UnicodeArabicKashida, '0');

            return compareInfo.Compare(text, pattern, options) == 0;
        }

        // Returns true iff two matching strings continue to match when kashida are considered.
        private static bool IsAlefHamzaMatch(string text, string pattern, CompareInfo compareInfo)
        {
            const CompareOptions options = CompareOptions.IgnoreSymbols | CompareOptions.StringSort | CompareOptions.IgnoreNonSpace;

            // Relace the AlefHamza chars with non-symbolic, non-diacritic constant values.
            // AlefHamza variations are ignored with the CompareOptions we use during the search.
            // When called, it's ok if the constant value matches other chars in the find pattern.
            // We've already got a match ignoring AlefHamza.

            text = text.Replace(UnicodeArabicAlefMaddaAbove, '0');
            text = text.Replace(UnicodeArabicAlefHamzaAbove, '1');
            text = text.Replace(UnicodeArabicAlefHamzaBelow, '2');

            pattern = pattern.Replace(UnicodeArabicAlefMaddaAbove, '0');
            pattern = pattern.Replace(UnicodeArabicAlefHamzaAbove, '1');
            pattern = pattern.Replace(UnicodeArabicAlefHamzaBelow, '2');

            return compareInfo.Compare(text, pattern, options) == 0;
        }

        /// <summary>
        /// Set the find text content from reading the text on the current text position.
        /// </summary>
        /// <returns>
        /// Returns the number of characters actually loaded into the findText array.
        /// </returns>
        private static int SetFindTextAndFindTextPositionMap(
            ITextPointer startPosition, 
            ITextPointer endPosition, 
            ITextPointer navigator, 
            LogicalDirection direction,
            bool matchLast,
            char[] findText,
            int[] findTextPositionMap)
        {
            Invariant.Assert(startPosition.CompareTo(navigator) <= 0);
            Invariant.Assert(endPosition.CompareTo(navigator) >= 0);

            int runCount;
            int inlineCount = 0;
            int findTextLength = 0;

            // Set the first offset which is zero on TextBufferSize + 1 location of 
            // the text position map in case of the backward searching
            if (matchLast && findTextLength == 0)
            {
                findTextPositionMap[findTextPositionMap.Length - 1] = 0;
            }

            while ((matchLast ? startPosition.CompareTo(navigator) : navigator.CompareTo(endPosition)) < 0)
            {
                switch (navigator.GetPointerContext(direction))
                {
                    case TextPointerContext.Text:
                        runCount = navigator.GetTextRunLength(direction);
                        runCount = Math.Min(runCount, findText.Length - findTextLength);

                        if (!matchLast)
                        {
                            runCount = Math.Min(runCount, navigator.GetOffsetToPosition(endPosition));
                            navigator.GetTextInRun(direction, findText, findTextLength, runCount);

                            for (int i = findTextLength; i < findTextLength + runCount; i++)
                            {
                                findTextPositionMap[i] = i + inlineCount;
                            }
                        }
                        else
                        {
                            runCount = Math.Min(runCount, startPosition.GetOffsetToPosition(navigator));
                            navigator.GetTextInRun(
                                direction, 
                                findText,
                                findText.Length - findTextLength - runCount, 
                                runCount);

                            // Set the text offest for the amount of runCount from the last index
                            // of text position map
                            int mapIndex = findText.Length - findTextLength - 1;
                            for (int i = findTextLength; i < findTextLength + runCount; i++)
                            {
                                findTextPositionMap[mapIndex--] = i + inlineCount + 1;
                            }
                        }

                        // Move the navigator position for the amount of runCount
                        navigator.MoveByOffset(matchLast ? - runCount : runCount);
                        findTextLength += runCount;
                        break;

                    case TextPointerContext.None:
                    case TextPointerContext.ElementStart:
                    case TextPointerContext.ElementEnd:
                        if (IsAdjacentToFormatElement(navigator, direction))
                        {
                            // Filter out formatting tags since find text content is plain.
                            inlineCount++;
                        }
                        else
                        {
                            if (!matchLast)
                            {
                                // Stick in a line break to account for the block element.
                                findText[findTextLength] = '\n';
                                findTextPositionMap[findTextLength] = findTextLength + inlineCount;
                                findTextLength++;
                            }
                            else
                            {
                                // Increse the find text length first since adding text and map reversely
                                findTextLength++;

                                // Stick in a line break to account for the block element and
                                // add text offset on the last index of text position map
                                findText[findText.Length - findTextLength] = '\n';
                                findTextPositionMap[findText.Length - findTextLength] = findTextLength + inlineCount;
                            }
                        }

                        navigator.MoveToNextContextPosition(direction);
                        break;

                    case TextPointerContext.EmbeddedElement:
                        if (!matchLast)
                        {
                            findText[findTextLength] = '\xf8ff'; // Unicode private use.
                            findTextPositionMap[findTextLength] = findTextLength + inlineCount;
                            findTextLength++;
                        }
                        else
                        {
                            // Increse the find text length first since adding text and map reversely
                            findTextLength++;

                            // Set the private unicode value and text offset
                            findText[findText.Length - findTextLength] = '\xf8ff'; 
                            findTextPositionMap[findText.Length - findTextLength] = findTextLength + inlineCount;
                        }

                        navigator.MoveToNextContextPosition(direction);
                        break;
                }

                if (findTextLength >= findText.Length)
                {
                    break;
                }
            }

            // Complete the adding the find text position to the position map for only the forward finding.
            // The backward finding(matchLast) is already added initially as the zero offset at the end of
            // text position map.
            if (!matchLast)
            {
                if (findTextLength > 0)
                {
                    findTextPositionMap[findTextLength] = findTextPositionMap[findTextLength - 1] + 1;
                }
                else
                {
                    findTextPositionMap[0] = 0;
                }
            }

            return findTextLength;
        }


        // Initialize bidi flags that check the bidi and alef character.
        internal static void InitializeBidiFlags(
            string textString, 
            out bool stringContainedBidiCharacter, 
            out bool stringContainedAlefCharacter)
        {
            stringContainedBidiCharacter = false;
            stringContainedAlefCharacter = false;

            for (int index = 0; index < textString.Length; index++)
            {
                char currentChar = textString[index];

                if (currentChar >= UnicodeBidiStart && currentChar <= UnicodeBidiEnd)
                {
                    stringContainedBidiCharacter = true;

                    if (currentChar == UnicodeArabicAlefMaddaAbove ||
                        currentChar == UnicodeArabicAlefHamzaAbove ||
                        currentChar == UnicodeArabicAlefHamzaBelow ||
                        currentChar == UnicodeArabicAlef)
                    {
                        stringContainedAlefCharacter = true;
                        break;
                    }
                }
            }
        }

        // Replace arabic alef-hamza character with arabic alef character so that
        // ignore alef-hamza from the comparing string.
        internal static string ReplaceAlefHamzaWithAlef(string textString)
        {
            // Replace alef-hamza with alef.
            textString = textString.Replace(UnicodeArabicAlefMaddaAbove, UnicodeArabicAlef);
            textString = textString.Replace(UnicodeArabicAlefHamzaAbove, UnicodeArabicAlef);
            textString = textString.Replace(UnicodeArabicAlefHamzaBelow, UnicodeArabicAlef);

            return textString;
        }

        /// <summary>
        /// Return true if the match index is position of the word boundary.
        /// </summary>
        private static bool IsAtWordBoundary(string textString, int matchIndex, int matchLength, bool hasPreceedingSeparatorChar, bool hasFollowingSeparatorChar)
        {
            bool matchWholeWord = false;

            int textLength = textString.Length;

            Invariant.Assert(matchIndex + matchLength <= textLength);

            if (matchIndex == 0)
            {
                if (hasPreceedingSeparatorChar)
                {
                    if (matchIndex + matchLength < textLength)
                    {
                        if (IsSeparatorChar(textString[matchIndex + matchLength]))
                        {
                            matchWholeWord = true;
                        }
                    }
                    else if (hasFollowingSeparatorChar)
                    {
                        matchWholeWord = true;
                    }
                }
            }
            else if (matchIndex + matchLength == textLength)
            {
                if (IsSeparatorChar(textString[matchIndex - 1]) && hasFollowingSeparatorChar)
                {
                    matchWholeWord = true;
                }
            }
            else
            {
                if (IsSeparatorChar(textString[matchIndex - 1]) && IsSeparatorChar(textString[matchIndex + matchLength]))
                {
                    matchWholeWord = true;
                }
            }

            return matchWholeWord;
        }

        /// <summary>
        /// Return true if the character is the separator as the WhiteSpace, Punctuation, Symbol or Separator.
        /// </summary>
        // REVIEW:benwest:bug 1789184
        // Why don't we use the word breaker code in place of IsSeparatorChar?  I suspect we're broken with text like
        // abc[ideographic script chars] (ie, when two scripts are adjacent).  The SelectionWordBreaker code is much more sophisticated
        // and will catch things like this.
        private static bool IsSeparatorChar(char separatorChar)
        {
            if (Char.IsWhiteSpace(separatorChar) ||
                Char.IsPunctuation(separatorChar) ||
                Char.IsSymbol(separatorChar) ||
                Char.IsSeparator(separatorChar))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Returns true if pointer preceeds an Inline start or end edge.
        /// </summary>
        private static bool IsAdjacentToFormatElement(ITextPointer pointer, LogicalDirection direction)
        {
            TextPointerContext context;
            bool isAdjacentToFormatElement;

            isAdjacentToFormatElement = false;

            if (direction == LogicalDirection.Forward)
            {
                context = pointer.GetPointerContext(LogicalDirection.Forward);

                if (context == TextPointerContext.ElementStart &&
                    TextSchema.IsFormattingType(pointer.GetElementType(LogicalDirection.Forward)))
                {
                    isAdjacentToFormatElement = true;
                }
                else if (context == TextPointerContext.ElementEnd &&
                         TextSchema.IsFormattingType(pointer.ParentType))
                {
                    isAdjacentToFormatElement = true;
                }
            }
            else
            {
                context = pointer.GetPointerContext(LogicalDirection.Backward);

                if (context == TextPointerContext.ElementEnd &&
                    TextSchema.IsFormattingType(pointer.GetElementType(LogicalDirection.Backward)))
                {
                    isAdjacentToFormatElement = true;
                }
                else if (context == TextPointerContext.ElementStart &&
                         TextSchema.IsFormattingType(pointer.ParentType))
                {
                    isAdjacentToFormatElement = true;
                }
            }

            return isAdjacentToFormatElement;
        }

        #endregion Private Methods

        //---------------------------------------------------------------------
        //
        // Private Fields
        //
        //---------------------------------------------------------------------

        #region Private Fields

        private const int TextBlockLength = 64;
                                                                                    
        private const char UnicodeBidiStart                 = '\u0590';
        private const char UnicodeBidiEnd                   = '\u07bf';

        private const char UnicodeArabicKashida             = '\u0640';
        private const char UnicodeArabicAlefMaddaAbove      = '\u0622';
        private const char UnicodeArabicAlefHamzaAbove      = '\u0623';
        private const char UnicodeArabicAlefHamzaBelow      = '\u0625';
        private const char UnicodeArabicAlef                = '\u0627';

        #endregion Private Fields
    }
}
