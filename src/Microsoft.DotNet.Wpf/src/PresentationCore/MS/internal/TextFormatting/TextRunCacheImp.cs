// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
//  Contents:  Cache of text and text properties of run
//
//


using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Diagnostics;
using System.Windows.Media.TextFormatting;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace MS.Internal.TextFormatting
{
    /// <summary>
    /// TextFormatter caches runs it receives from GetTextRun callback. This cache 
    /// object is managed by text layout client. 
    /// 
    /// This method is used to improve performance in application whose fetching the 
    /// run has significant performance implication. Application using this caching 
    /// mechanism is responsible for invalidating the content in the cache when 
    /// its changed.
    /// </summary>
    internal sealed class TextRunCacheImp
    {
        private SpanVector      _textRunVector;         // text run vector indexed by cp
        private SpanPosition    _latestPosition;


        /// <summary>
        /// Default constructor used internally
        /// </summary>
        /// <remarks>
        /// Text layout client never create their own run cache as its implementation
        /// should be opaque to them.
        /// </remarks>
        internal TextRunCacheImp()
        {
            _textRunVector = new SpanVector(null);
            _latestPosition = new SpanPosition();
        }


        /// <summary>
        /// Client to notify change in part of the cache when text or 
        /// properties of the run is being added, removed or replaced.
        /// </summary>
        /// <remarks>
        /// The client's expectation of this notification is that TextFormatter
        /// retains valid state for run cache in response to this change. It's 
        /// expected that at least TextFormatter will refetch the runs affected
        /// by the change. Subsequent runs may or may not be refected depending
        /// on the state of the cache after the change. 
        /// </remarks>
        public void Change(
            int     textSourceCharacterIndex,
            int     addition,
            int     removal
            )
        {
            if (textSourceCharacterIndex < 0)
                return;

            int cchActive = 0;
            for (int i = 0; i < _textRunVector.Count; i++)
                cchActive += _textRunVector[i].length;

            if (textSourceCharacterIndex >= cchActive)
                return;

            SpanRider textRunSpanRider = new SpanRider(_textRunVector, _latestPosition, textSourceCharacterIndex);
            _latestPosition = textRunSpanRider.SpanPosition;

            // we remove runs from the cache starting from the one containing the change
            // to the end of the active range. We do not try to interpret the scope of
            // the change and try to minimize the range in which the cache is invalidated,
            // because that would require an in-depth understanding of how our client 
            // implement their formatting change mechanism and how they respond to future 
            // refetch after the change which could vary among different clients. That 
            // kind of work is beyond the purpose of this notification.

            _latestPosition = _textRunVector.SetValue(
                textRunSpanRider.CurrentSpanStart,
                cchActive - textRunSpanRider.CurrentSpanStart,
                _textRunVector.Default,
                _latestPosition
                );
        }


        /// <summary>
        /// Fetch cached textrun
        /// </summary>
        internal TextRun FetchTextRun(
            FormatSettings          settings,
            int                     cpFetch,
            int                     cpFirst,
            out int                 offsetToFirstCp,
            out int                 runLength
            )
        {
            SpanRider textRunSpanRider = new SpanRider(_textRunVector, _latestPosition, cpFetch);
            _latestPosition = textRunSpanRider.SpanPosition;
            TextRun textRun = (TextRun)textRunSpanRider.CurrentElement;

            if(textRun == null)
            {
                // run not already cached, fetch new run and cache it

                textRun = settings.TextSource.GetTextRun(cpFetch);

                if (textRun.Length < 1)
                {
                    throw new ArgumentOutOfRangeException("textRun.Length", SR.Get(SRID.ParameterMustBeGreaterThanZero));
                }

                Plsrun plsrun = TextRunInfo.GetRunType(textRun);

                if (plsrun == Plsrun.Text || plsrun == Plsrun.InlineObject)
                {
                    TextRunProperties properties = textRun.Properties;

                    if (properties == null)
                        throw new ArgumentException(SR.Get(SRID.TextRunPropertiesCannotBeNull));

                    if (properties.FontRenderingEmSize <= 0)
                        throw new ArgumentException(SR.Get(SRID.PropertyOfClassMustBeGreaterThanZero, "FontRenderingEmSize", "TextRunProperties"));

                    double realMaxFontRenderingEmSize = Constants.RealInfiniteWidth / Constants.GreatestMutiplierOfEm;

                    if (properties.FontRenderingEmSize > realMaxFontRenderingEmSize)
                        throw new ArgumentException(SR.Get(SRID.PropertyOfClassCannotBeGreaterThan, "FontRenderingEmSize", "TextRunProperties", realMaxFontRenderingEmSize));

                    CultureInfo culture = CultureMapper.GetSpecificCulture(properties.CultureInfo);

                    if (culture == null)
                        throw new ArgumentException(SR.Get(SRID.PropertyOfClassCannotBeNull, "CultureInfo", "TextRunProperties"));

                    if (properties.Typeface == null)
                        throw new ArgumentException(SR.Get(SRID.PropertyOfClassCannotBeNull, "Typeface", "TextRunProperties"));
                }


                //
                // TextRun is specifial to SpanVector because TextRun also encodes position which needs to be 
                // consistent with the positions encoded by SpanVector. In run cache, the begining of a span 
                // should always correspond to the begining of a cached text run. If the end of the currently fetched 
                // run overlaps with the begining of an already cached run, the begining of the cached run needs to be 
                // adjusted as well as its span. Because we can't gurantee the correctness of the overlapped range 
                // so we'll simply remove the overlapped runs here.
                //                

                // Move the rider to the end of the current run
                textRunSpanRider.At(cpFetch + textRun.Length - 1);
                _latestPosition = textRunSpanRider.SpanPosition;

                if (textRunSpanRider.CurrentElement != _textRunVector.Default)
                {
                    // The end overlaps with one or more cached runs, clear the range from the 
                    // begining of the current fetched run to the end of the last overlapped cached run. 
                    _latestPosition = _textRunVector.SetReference(
                        cpFetch, 
                        textRunSpanRider.CurrentPosition + textRunSpanRider.Length - cpFetch, 
                        _textRunVector.Default,
                        _latestPosition
                        );
                }

                _latestPosition = _textRunVector.SetReference(cpFetch, textRun.Length, textRun, _latestPosition);

                // Refresh the rider's SpanPosition following previous SpanVector.SetReference calls
                textRunSpanRider.At(_latestPosition, cpFetch);
            }

            // If the TextRun was obtained from the cache, make sure it has the right PixelsPerDip set on its properties.

            if (textRun.Properties != null)
            {
                textRun.Properties.PixelsPerDip = settings.TextSource.PixelsPerDip;
            }

            offsetToFirstCp = textRunSpanRider.CurrentPosition - textRunSpanRider.CurrentSpanStart;
            runLength = textRunSpanRider.Length;
            Debug.Assert(textRun != null && runLength > 0, "Invalid run!");

            bool isText = textRun is ITextSymbols;

            if (isText)
            {
                // Chop text run to optimal length so we dont spend forever analysing
                // them all at once. 
                
                int looseCharLength = TextStore.TypicalCharactersPerLine - cpFetch + cpFirst;                

                if(looseCharLength <= 0)
                {
                    // this line already exceeds typical line length, incremental fetch goes
                    // about a quarter of the typical length.

                    looseCharLength = (int)Math.Round(TextStore.TypicalCharactersPerLine * 0.25);
                }

                if(runLength > looseCharLength)
                {
                    if (TextRunInfo.GetRunType(textRun) == Plsrun.Text)
                    {
                        // 
                        // When chopping the run at the typical line length, 
                        // - don't chop in between of higher & lower surrogate
                        // - don't chop combining mark away from its base character
                        // - don't chop joiner from surrounding characters
                        // 
                        // Starting from the initial chopping point, we look ahead to find a safe position. We stop at 
                        // a limit in case the run consists of many combining mark & joiner. That is rare and doesn't make
                        // much sense in shaping already. 
                        // 
                        
                        CharacterBufferReference charBufferRef = textRun.CharacterBufferReference;

                        // We look ahead by one more line at most. It is not normal to have
                        // so many combining mark or joiner characters in a row. It doesn't make sense to 
                        // look further if so.
                        int lookAheadLimit = Math.Min(runLength, looseCharLength + TextStore.TypicalCharactersPerLine);
                        
                        int sizeOfChar = 0; 
                        int endOffset  = 0;                        
                        bool canBreakAfterPrecedingChar = false;
                        
                        for (endOffset = looseCharLength - 1; endOffset < lookAheadLimit; endOffset += sizeOfChar)
                        {
                            CharacterBufferRange charString = new CharacterBufferRange(
                                charBufferRef.CharacterBuffer,
                                charBufferRef.OffsetToFirstChar + offsetToFirstCp + endOffset, 
                                runLength - endOffset 
                                );                                

                            int ch = Classification.UnicodeScalar(charString, out sizeOfChar);                            

                            // We can only safely break if the preceding char is not a joiner character (i.e. can-break-after), 
                            // and the current char is not combining or joiner (i.e. can-break-before). 
                            if (canBreakAfterPrecedingChar && !Classification.IsCombining(ch) && !Classification.IsJoiner(ch) )
                            {
                                break; 
                            }

                            canBreakAfterPrecedingChar = !Classification.IsJoiner(ch);
                        }

                        looseCharLength = Math.Min(runLength, endOffset);
                    }
                    
                    runLength = looseCharLength;
                }
            }


            Debug.Assert(

                // valid run found
                runLength > 0

                // non-text run always fetched at run start
                &&  (   isText
                    || textRunSpanRider.CurrentSpanStart - textRunSpanRider.CurrentPosition == 0)

                // span rider of both text and format point to valid position
                &&  (textRunSpanRider.Length > 0 && textRunSpanRider.CurrentElement != null),

                "Text run fetching error!"
                );

            return textRun;
        }


        /// <summary>
        /// Get text immediately preceding cpLimit.
        /// </summary>
        internal TextSpan<CultureSpecificCharacterBufferRange> GetPrecedingText(TextSource textSource, int cpLimit)
        {
            if (cpLimit > 0)
            {
                SpanRider textRunSpanRider = new SpanRider(_textRunVector, _latestPosition);
                if (textRunSpanRider.At(cpLimit - 1))
                {
                    CharacterBufferRange charString = CharacterBufferRange.Empty;                    
                    CultureInfo culture = null;
                    
                    TextRun run = textRunSpanRider.CurrentElement as TextRun;

                    if (run != null)
                    {
                        // Only TextRun containing text would have non-empty Character buffer range.
                        if ( TextRunInfo.GetRunType(run) == Plsrun.Text
                          && run.CharacterBufferReference.CharacterBuffer != null)
                        {   
                            charString = new CharacterBufferRange(
                                run.CharacterBufferReference,
                                cpLimit - textRunSpanRider.CurrentSpanStart);

                            culture = CultureMapper.GetSpecificCulture(run.Properties.CultureInfo);                                
                        }
                    
                        return new TextSpan<CultureSpecificCharacterBufferRange>(
                            cpLimit - textRunSpanRider.CurrentSpanStart, // cp length
                            new CultureSpecificCharacterBufferRange(culture, charString)                                   
                         );                                                    
                    }
                }
            }

            // not in cache so call back to client
            return textSource.GetPrecedingText(cpLimit);
        }

        /// <summary>
        /// Return all TextRuns cached. If for a particular range there is no TextRun cached, the TextSpan would 
        /// contain null TextRun object.
        /// </summary>
        internal IList<TextSpan<TextRun>> GetTextRunSpans()
        {
            IList<TextSpan<TextRun>> textRunList = new List<TextSpan<TextRun>>(_textRunVector.Count);

            for (int i = 0; i < _textRunVector.Count; i++)
            {
                Span currentSpan = _textRunVector[i];                
                textRunList.Add(new TextSpan<TextRun>(currentSpan.length, currentSpan.element as TextRun));
            }            

            return textRunList;
        }
    }
}
