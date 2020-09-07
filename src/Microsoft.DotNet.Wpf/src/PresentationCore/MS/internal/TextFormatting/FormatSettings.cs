// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
//  Contents:  Settings to format text
//
//


using System;
using System.Diagnostics;
using System.Security;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;


namespace MS.Internal.TextFormatting
{
    /// <summary>
    /// Text formatting state
    /// </summary>
    internal sealed class FormatSettings
    {
        private TextFormatterImp    _formatter;                 // formatter object
        private TextSource          _textSource;                // client text source for main text
        private TextRunCacheImp     _runCache;                  // run cache owned by client
        private ParaProp            _pap;                       // current paragraph properties
        private DigitState          _digitState;                // current number substitution properties
        private TextLineBreak       _previousLineBreak;         // break of previous line
        private int                 _maxLineWidth;              // Max line width of the line being built. Only used in Optimal paragraph for the moment
        private int                 _textIndent;                // Distance from line start to where text starts

        private TextFormattingMode  _textFormattingMode;
        private bool                _isSideways;



        internal FormatSettings(
            TextFormatterImp    formatter,
            TextSource          textSource,
            TextRunCacheImp     runCache,
            ParaProp            pap,
            TextLineBreak       previousLineBreak,
            bool                isSingleLineFormatting,
            TextFormattingMode  textFormattingMode,
            bool                isSideways
            )
        {
            _isSideways     = isSideways;
            _textFormattingMode = textFormattingMode;
            _formatter      = formatter;
            _textSource     = textSource;
            _runCache       = runCache;
            _pap            = pap;
            _digitState     = new DigitState();
            _previousLineBreak = previousLineBreak;
            _maxLineWidth      = Constants.IdealInfiniteWidth;

            if (isSingleLineFormatting)
            {
                // Apply text indent on each line in single line mode
                _textIndent = _pap.Indent;
            }
        }

        internal TextFormattingMode TextFormattingMode
        {
            get
            {
                return _textFormattingMode;
            }
        }

        internal bool IsSideways
        {
            get
            {
                return _isSideways;
            }
        }
        /// <summary>
        /// Current formatter
        /// </summary>
        internal TextFormatterImp Formatter
        {
            get { return _formatter; }
        }


        /// <summary>
        /// Current source
        /// </summary>
        internal TextSource TextSource
        {
            get { return _textSource; }
        }


        /// <summary>
        /// Break of previous line
        /// </summary>
        internal TextLineBreak PreviousLineBreak
        {
            get { return _previousLineBreak; }
        }


        /// <summary>
        /// paragraph properties
        /// </summary>
        internal ParaProp Pap
        {
            get { return _pap; }
        }

        /// <summary>
        /// Max width of the line being built. In optimal break mode, client may format
        /// breakpoints for the line with max width different from the paragraph width.
        /// </summary>
        internal int MaxLineWidth
        {
            get { return _maxLineWidth; }
        }

        /// <summary>
        /// Update formatting parameters at line start
        /// </summary>
        internal void UpdateSettingsForCurrentLine(
            int             maxLineWidth,
            TextLineBreak   previousLineBreak,
            bool            isFirstLineInPara
            )
        {
            _previousLineBreak = previousLineBreak;
            _digitState = new DigitState(); // reset digit state
            _maxLineWidth = Math.Max(maxLineWidth, 0);

            if (isFirstLineInPara)
            {
                _textIndent = _pap.Indent;
            }
            else
            {
                // Do not apply text indentation to all but the first line in para
                _textIndent = 0;
            }
        }


        /// <summary>
        /// Calculate formatting width from specified constraint width
        /// </summary>
        internal int GetFormatWidth(int finiteFormatWidth)
        {
            // LS does not support no wrap, for such case, we would need to
            // simulate formatting a line within an infinite paragraph width
            // while still keeping the last legit boundary for trimming purpose.
            return  _pap.Wrap ? finiteFormatWidth : Constants.IdealInfiniteWidth;
        }


        /// <summary>
        /// Calculate finite formatting width from specified constraint width
        /// </summary>
        internal int GetFiniteFormatWidth(int paragraphWidth)
        {
            // indent is part of our text line but not of LS line
            // paragraph width == 0 means format width is unlimited
            int formatWidth = (paragraphWidth <= 0 ? Constants.IdealInfiniteWidth : paragraphWidth);
            formatWidth = formatWidth - _pap.ParagraphIndent;
            
            // sanitize the format width value before passing to LS
            formatWidth = Math.Max(formatWidth, 0);
            formatWidth = Math.Min(formatWidth, Constants.IdealInfiniteWidth);
            return formatWidth;
        }

        /// <summary>
        /// Fetch text run and character string associated with it
        /// </summary>
        internal CharacterBufferRange FetchTextRun(
            int             cpFetch,
            int             cpFirst,
            out TextRun     textRun,
            out int         runLength
            )
        {
            int offsetToFirstCp;
            textRun = _runCache.FetchTextRun(this, cpFetch, cpFirst, out offsetToFirstCp, out runLength);

            CharacterBufferRange charString;

            switch (TextRunInfo.GetRunType(textRun))
            {
                case Plsrun.Text:
                {
                    CharacterBufferReference charBufferRef = textRun.CharacterBufferReference;

                    charString = new CharacterBufferRange(
                        charBufferRef.CharacterBuffer,
                        charBufferRef.OffsetToFirstChar + offsetToFirstCp,
                        runLength
                        );

                    break;
                }

                case Plsrun.InlineObject:                   
                    Debug.Assert(offsetToFirstCp == 0);
                    unsafe
                    {
                        charString = new CharacterBufferRange((char*) TextStore.PwchObjectReplacement, 1);
                    }
                    break;

                case Plsrun.LineBreak:
                    Debug.Assert(offsetToFirstCp == 0);
                    unsafe
                    {
                        //
                        // Line break is to be represented as "Line Separator" such that 
                        // it doesn't terminate the optimal paragraph formatting session, but still breaks the line
                        // unambiguously. 
                        //
                        charString = new CharacterBufferRange((char*) TextStore.PwchLineSeparator, 1);
                    }
                    break;       
                    
                case Plsrun.ParaBreak:
                    Debug.Assert(offsetToFirstCp == 0);
                    unsafe
                    {
                        // 
                        // Paragraph break is special as it also terminates the paragraph. 
                        // It should be represented as "Paragraph Separator" such that it will be correctly handled
                        // in Bidi and Optimal paragraph formatting.
                        //
                        charString = new CharacterBufferRange((char*) TextStore.PwchParaSeparator, 1);
                    }       
                    break;
                    
                case Plsrun.Hidden:
                    unsafe
                    {
                        charString = new CharacterBufferRange((char*) TextStore.PwchHidden, 1);
                    }
                    break;

                default:
                    charString = CharacterBufferRange.Empty;
                    break;
            }

            return charString;
        }


        /// <summary>
        /// Get text immediately preceding cpLimit.
        /// </summary>
        internal TextSpan<CultureSpecificCharacterBufferRange> GetPrecedingText(int cpLimit)
        {
            return _runCache.GetPrecedingText(_textSource, cpLimit);
        }


        /// <summary>
        /// Current number substitution propeties
        /// </summary>
        internal DigitState DigitState
        {
            get { return _digitState; }
        }


        /// <summary>
        /// Distance from line start to where text starts
        /// </summary>
        internal int TextIndent
        {
            get { return _textIndent; }
        }
    }
}
