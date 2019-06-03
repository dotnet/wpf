// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
//  Contents:  Properties of text, text line and paragraph
//
//


using System;
using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using MS.Internal;
using MS.Internal.Shaping;

namespace MS.Internal.TextFormatting
{
    /// <summary>
    /// Internal paragraph properties wrapper
    /// </summary>
    internal sealed class ParaProp
    {
        /// <summary>
        /// Constructing paragraph properties
        /// </summary>
        /// <param name="formatter">Text formatter</param>
        /// <param name="paragraphProperties">paragraph properties</param>
        /// <param name="optimalBreak">produce optimal break</param>
        internal ParaProp(
            TextFormatterImp        formatter,
            TextParagraphProperties paragraphProperties,
            bool                    optimalBreak
            )
        {
            _paragraphProperties = paragraphProperties;

            _emSize = TextFormatterImp.RealToIdeal(paragraphProperties.DefaultTextRunProperties.FontRenderingEmSize);
            _indent = TextFormatterImp.RealToIdeal(paragraphProperties.Indent);
            _paragraphIndent = TextFormatterImp.RealToIdeal(paragraphProperties.ParagraphIndent);
            _height = TextFormatterImp.RealToIdeal(paragraphProperties.LineHeight);

            if (_paragraphProperties.FlowDirection == FlowDirection.RightToLeft)
            {
                _statusFlags |= StatusFlags.Rtl;
            }

            if (optimalBreak)
            {
                _statusFlags |= StatusFlags.OptimalBreak;
            }
        }


        [Flags]
        private enum StatusFlags
        {
            Rtl             = 0x00000001,   // right-to-left reading
            OptimalBreak    = 0x00000002,   // produce optimal break
        }

        internal bool RightToLeft
        {
            get { return (_statusFlags & StatusFlags.Rtl) != 0; }
        }

        internal bool OptimalBreak
        {
            get { return (_statusFlags & StatusFlags.OptimalBreak) != 0; }
        }

        internal bool FirstLineInParagraph
        {
            get { return _paragraphProperties.FirstLineInParagraph; }
        }

        internal bool AlwaysCollapsible
        {
            get { return _paragraphProperties.AlwaysCollapsible; }
        }

        internal int Indent
        {
            get { return _indent; }
        }

        internal int ParagraphIndent
        {
            get { return _paragraphIndent; }
        }

        internal double DefaultIncrementalTab
        {
            get { return _paragraphProperties.DefaultIncrementalTab; }
        }

        internal IList<TextTabProperties> Tabs
        {
            get { return _paragraphProperties.Tabs; }
        }

        internal TextAlignment Align
        {
            get { return _paragraphProperties.TextAlignment; }
        }

        internal bool Justify
        {
            get { return _paragraphProperties.TextAlignment == TextAlignment.Justify; }
        }

        internal bool EmergencyWrap
        {
            get { return _paragraphProperties.TextWrapping == TextWrapping.Wrap; }
        }

        internal bool Wrap
        {
            get { return _paragraphProperties.TextWrapping == TextWrapping.WrapWithOverflow || EmergencyWrap; }
        }

        internal Typeface DefaultTypeface
        {
            get { return _paragraphProperties.DefaultTextRunProperties.Typeface; }
        }

        internal int EmSize
        {
            get { return _emSize; }
        }

        internal int LineHeight
        {
            get { return _height; }
        }

        internal TextMarkerProperties TextMarkerProperties
        {
            get { return _paragraphProperties.TextMarkerProperties; }
        }

        internal TextLexicalService Hyphenator
        {
            get { return _paragraphProperties.Hyphenator; }
        }

        internal TextDecorationCollection TextDecorations
        {
            get { return _paragraphProperties.TextDecorations; }
        }

        internal Brush DefaultTextDecorationsBrush
        {
            get { return _paragraphProperties.DefaultTextRunProperties.ForegroundBrush; }
        }

        private StatusFlags                 _statusFlags;
        private TextParagraphProperties     _paragraphProperties;
        private int                         _emSize;
        private int                         _indent;
        private int                         _paragraphIndent;
        private int                         _height;
    }



    /// <summary>
    /// State of textrun started when it's fetched
    /// </summary>
    internal sealed class TextRunInfo
    {
        private CharacterBufferRange _charBufferRange;
        private int                  _textRunLength;
        private int                  _offsetToFirstCp;
        private TextRun              _textRun;
        private Plsrun               _plsrun;
        private CultureInfo          _digitCulture;
        private ushort               _charFlags;
        private ushort               _runFlags;
        private TextModifierScope    _modifierScope;
        private TextRunProperties    _properties;

        /// <summary>
        /// Constructing a textrun info
        /// </summary>
        /// <param name="charBufferRange">characte buffer range for the run</param>
        /// <param name="textRunLength">textrun length</param>
        /// <param name="offsetToFirstCp">character offset to run first cp</param>
        /// <param name="textRun">text run</param>
        /// <param name="lsRunType">the internal LS run type </param>
        /// <param name="charFlags">character attribute flags</param>
        /// <param name="digitCulture">digit culture for the run</param>
        /// <param name="contextualSubstitution">contextual number substitution for the run</param>
        /// <param name="symbolTypeface">if true, indicates a text run in a symbol (i.e., non-Unicode) font</param>
        /// <param name="modifierScope">The current TextModifier scope for this TextRunInfo</param>        
        internal TextRunInfo(
            CharacterBufferRange charBufferRange,
            int                  textRunLength,
            int                  offsetToFirstCp,
            TextRun              textRun,
            Plsrun               lsRunType,
            ushort               charFlags,
            CultureInfo          digitCulture,
            bool                 contextualSubstitution,
            bool                 symbolTypeface,
            TextModifierScope    modifierScope
            )
        {
            _charBufferRange = charBufferRange;
            _textRunLength = textRunLength;
            _offsetToFirstCp = offsetToFirstCp;
            _textRun = textRun;
            _plsrun = lsRunType;
            _charFlags = charFlags;
            _digitCulture = digitCulture;
            _runFlags = 0;
            _modifierScope = modifierScope;

            if (contextualSubstitution)
            {
                _runFlags |= (ushort)RunFlags.ContextualSubstitution;
            }

            if (symbolTypeface)
            {
                _runFlags |= (ushort)RunFlags.IsSymbol;
            }
        }


        /// <summary>
        /// Text run
        /// </summary>
        internal TextRun TextRun
        {
            get { return _textRun; }
        }

        /// <summary>
        /// The final TextRunProperties of the TextRun
        /// </summary>
        internal TextRunProperties Properties
        {
            get 
            {
                // The non-null value is cached 
                if (_properties == null)
                {
                    if (_modifierScope != null)
                    {
                        _properties = _modifierScope.ModifyProperties(_textRun.Properties);
                    }
                    else
                    {
                        _properties = _textRun.Properties;
                    }
                }

                return _properties;
            }
        }

        /// <summary>
        /// character buffer
        /// </summary>
        internal CharacterBuffer CharacterBuffer
        {
            get { return _charBufferRange.CharacterBuffer; }
        }


        /// <summary>
        /// Character offset to run first character in the buffer
        /// </summary>
        internal int OffsetToFirstChar
        {
            get { return _charBufferRange.OffsetToFirstChar; }
        }


        /// <summary>
        /// Character offset to run first cp from line start
        /// </summary>
        internal int OffsetToFirstCp
        {
            get { return _offsetToFirstCp; }
        }


        /// <summary>
        /// String length
        /// </summary>
        internal int StringLength
        {
            get { return _charBufferRange.Length; }
            set { _charBufferRange = new CharacterBufferRange(_charBufferRange.CharacterBufferReference, value); }
        }


        /// <summary>
        /// Run length
        /// </summary>
        internal int Length
        {
            get { return _textRunLength; }
            set { _textRunLength = value; }
        }


        /// <summary>
        /// State of character in the run
        /// </summary>
        internal ushort CharacterAttributeFlags
        {
            get { return _charFlags; }
            set { _charFlags = value; }
        }


        /// <summary>
        /// Digit culture for the run.
        /// </summary>
        internal CultureInfo DigitCulture
        {
            get { return _digitCulture; }
        }


        /// <summary>
        /// Specifies whether the run requires contextual number substitution.
        /// </summary>
        internal bool ContextualSubstitution
        {
            get { return (_runFlags & (ushort)RunFlags.ContextualSubstitution) != 0; }
        }


        /// <summary>
        /// Specifies whether the run is in a non-Unicode font, such as Symbol or Wingdings.
        /// </summary>
        /// <remarks>
        /// Non-Unicode runs require special handling because code points do not have their
        /// standard Unicode meanings.
        /// </remarks>
        internal bool IsSymbol
        {
            get { return (_runFlags & (ushort)RunFlags.IsSymbol) != 0; }
        }


        /// <summary>
        /// Plsrun type
        /// </summary>
        internal Plsrun Plsrun
        {
            get { return _plsrun; }
        }


        /// <summary>
        /// Is run an end of line?
        /// </summary>
        internal bool IsEndOfLine
        {
            get { return _textRun is TextEndOfLine; }
        }

        /// <summary>
        /// The modification scope of this run
        /// </summary>
        internal TextModifierScope TextModifierScope 
        {
            get { return _modifierScope; }
        }
    

        /// <summary>
        /// Get rough width of the run
        /// </summary>
        internal int GetRoughWidth(double realToIdeal)
        {
            TextRunProperties properties = _textRun.Properties;

            if (properties != null)
            {
                // estimate rough width of each character in a run being 75% of Em.
                return (int)Math.Round(properties.FontRenderingEmSize * 0.75 * _textRunLength * realToIdeal);
            }
            return 0;
        }


        /// <summary>
        /// Map TextRun type to known plsrun type
        /// </summary>
        internal static Plsrun GetRunType(TextRun textRun)
        {
            if (textRun is ITextSymbols || textRun is TextShapeableSymbols)
                return Plsrun.Text;

            if (textRun is TextEmbeddedObject)
                return Plsrun.InlineObject;

            if (textRun is TextEndOfParagraph)
                return Plsrun.ParaBreak;

            if (textRun is TextEndOfLine)
                return Plsrun.LineBreak;

            // Other text run type are all considered hidden by LS
            return Plsrun.Hidden;
        }


        [Flags]
        private enum RunFlags
        {
            ContextualSubstitution  = 0x0001,
            IsSymbol                = 0x0002,
        }
    }
}

