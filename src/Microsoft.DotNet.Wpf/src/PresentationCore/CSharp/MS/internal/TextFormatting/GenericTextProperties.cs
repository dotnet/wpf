// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//+-----------------------------------------------------------------------
//
//
//
//  Contents:  Generic implementation of TextFormatter abstract classes
//
//

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using System.Globalization;


namespace MS.Internal.TextFormatting
{
    /// <summary>
    /// Generic implementation of TextRunProperties
    /// </summary>
    internal sealed class GenericTextRunProperties : TextRunProperties
    {
        /// <summary>
        /// Constructing TextRunProperties
        /// </summary>
        /// <param name="typeface">typeface</param>
        /// <param name="size">text size</param>
        /// <param name="hintingSize">text size for Truetype hinting program</param>
        /// <param name="culture">text culture info</param>
        /// <param name="textDecorations">TextDecorations </param>
        /// <param name="foregroundBrush">text foreground brush</param>
        /// <param name="backgroundBrush">highlight background brush</param>
        /// <param name="baselineAlignment">text vertical alignment to its container</param>
        /// <param name="substitution">number substitution behavior to apply to the text; can be null,
        /// in which case the default number substitution method for the text culture is used</param>
        public GenericTextRunProperties(
            Typeface                 typeface,
            double                   size,
            double                   hintingSize,
            double                   pixelsPerDip,
            TextDecorationCollection textDecorations,
            Brush                    foregroundBrush,
            Brush                    backgroundBrush,
            BaselineAlignment        baselineAlignment,
            CultureInfo              culture,
            NumberSubstitution       substitution
        )
        {
             _typeface = typeface;
            _emSize = size;
            _emHintingSize = hintingSize;
            _textDecorations = textDecorations;
            _foregroundBrush = foregroundBrush;
            _backgroundBrush = backgroundBrush;
            _baselineAlignment = baselineAlignment;
            _culture = culture;
            _numberSubstitution = substitution;
            PixelsPerDip = pixelsPerDip;
        }

        /// <summary>
        /// Hash code generator
        /// </summary>
        /// <returns>TextRunProperties hash code</returns>
        public override int GetHashCode()
        {
            return
                    _typeface.GetHashCode()
                ^ _emSize.GetHashCode()
                ^ _emHintingSize.GetHashCode()
                ^ ((_foregroundBrush == null) ? 0 : _foregroundBrush.GetHashCode())
                ^ ((_backgroundBrush == null) ? 0 : _backgroundBrush.GetHashCode())
                ^ ((_textDecorations == null) ? 0 : _textDecorations.GetHashCode())
                ^ ((int)_baselineAlignment << 3)
                ^ ((int)_culture.GetHashCode() << 6)
                ^ ((_numberSubstitution == null) ? 0 : _numberSubstitution.GetHashCode());
        }



        /// <summary>
        /// Equality check
        /// </summary>
        /// <returns>objects equals</returns>
        public override bool Equals(object o)
        {
            if ((o == null) || !(o is TextRunProperties))
            {
                return false;
            }

            TextRunProperties textRunProperties = (TextRunProperties)o;

            return
                    _emSize == textRunProperties.FontRenderingEmSize
                && _emHintingSize == textRunProperties.FontHintingEmSize
                && _culture == textRunProperties.CultureInfo
                && _typeface.Equals(textRunProperties.Typeface)
                && ((_textDecorations == null) ? textRunProperties.TextDecorations == null : _textDecorations.ValueEquals(textRunProperties.TextDecorations))
                && _baselineAlignment == textRunProperties.BaselineAlignment
                && ((_foregroundBrush == null) ? (textRunProperties.ForegroundBrush == null) : (_foregroundBrush.Equals(textRunProperties.ForegroundBrush)))
                && ((_backgroundBrush == null) ? (textRunProperties.BackgroundBrush == null) : (_backgroundBrush.Equals(textRunProperties.BackgroundBrush)))
                && ((_numberSubstitution == null) ? (textRunProperties.NumberSubstitution == null) : (_numberSubstitution.Equals(textRunProperties.NumberSubstitution)));
        }



        /// <summary>
        /// Run typeface
        /// </summary>
        public override Typeface Typeface
        {
            get { return _typeface; }
        }


        /// <summary>
        /// Em size of font used to format and display text
        /// </summary>
        public override double FontRenderingEmSize
        {
            get { return _emSize; }
        }


        /// <summary>
        /// Em size of font to determine subtle change in font hinting default value is 12pt
        /// </summary>
        public override double FontHintingEmSize
        {
            get { return _emHintingSize; }
        }


        /// <summary>
        /// Run text decoration
        /// </summary>
        public override TextDecorationCollection TextDecorations
        {
            get { return _textDecorations; }
        }

        /// <summary>
        /// Run text foreground brush
        /// </summary>
        public override Brush ForegroundBrush
        {
            get { return _foregroundBrush; }
        }


        /// <summary>
        /// Run text highlight background brush
        /// </summary>
        public override Brush BackgroundBrush
        {
            get { return _backgroundBrush; }
        }


        /// <summary>
        /// Run vertical box alignment
        /// </summary>
        public override BaselineAlignment BaselineAlignment
        {
            get { return _baselineAlignment; }
        }


        /// <summary>
        /// Run text Culture Info
        /// </summary>
        public override CultureInfo CultureInfo
        {
            get { return _culture; }
        }

        /// <summary>
        /// Run typography properties
        /// </summary>
        public override TextRunTypographyProperties TypographyProperties
        {
            get{return null;}
        }

        /// <summary>
        /// Run Text effects
        /// </summary>
        public override TextEffectCollection TextEffects
        {
            get { return null; }
        }


        /// <summary>
        /// Number substitution
        /// </summary>
        public override NumberSubstitution NumberSubstitution
        {
            get { return _numberSubstitution; }
        }

        private Typeface                 _typeface;
        private double                   _emSize;
        private double                   _emHintingSize;
        private TextDecorationCollection _textDecorations;
        private Brush                    _foregroundBrush;
        private Brush                    _backgroundBrush;
        private BaselineAlignment        _baselineAlignment;
        private CultureInfo              _culture;
        private NumberSubstitution       _numberSubstitution;
    }



    /// <summary>
    /// Generic implementation of TextParagraphProperties
    /// </summary>
    internal sealed class GenericTextParagraphProperties : TextParagraphProperties
    {
        /// <summary>
        /// Constructing TextParagraphProperties
        /// </summary>
        /// <param name="flowDirection">text flow direction</param>
        /// <param name="textAlignment">logical horizontal alignment</param>
        /// <param name="firstLineInParagraph">true if the paragraph is the first line in the paragraph</param>
        /// <param name="alwaysCollapsible">true if the line is always collapsible</param>
        /// <param name="defaultTextRunProperties">default paragraph's default run properties</param>
        /// <param name="textWrap">text wrap option</param>
        /// <param name="lineHeight">Paragraph line height</param>
        /// <param name="indent">line indentation</param>
        public GenericTextParagraphProperties(
            FlowDirection           flowDirection,
            TextAlignment           textAlignment,
            bool                    firstLineInParagraph,
            bool                    alwaysCollapsible,
            TextRunProperties       defaultTextRunProperties,
            TextWrapping            textWrap,
            double                  lineHeight,
            double                  indent
            )
        {
            _flowDirection = flowDirection;
            _textAlignment = textAlignment;
            _firstLineInParagraph = firstLineInParagraph;
            _alwaysCollapsible = alwaysCollapsible;
            _defaultTextRunProperties = defaultTextRunProperties;
            _textWrap = textWrap;
            _lineHeight = lineHeight;
            _indent = indent;
        }

        /// <summary>
        /// Constructing TextParagraphProperties from another one
        /// </summary>
        /// <param name="textParagraphProperties">source line props</param>
        public GenericTextParagraphProperties(TextParagraphProperties textParagraphProperties)
        {
            _flowDirection = textParagraphProperties.FlowDirection;
            _defaultTextRunProperties = textParagraphProperties.DefaultTextRunProperties;
            _textAlignment = textParagraphProperties.TextAlignment;
            _lineHeight = textParagraphProperties.LineHeight;
            _firstLineInParagraph = textParagraphProperties.FirstLineInParagraph;
            _alwaysCollapsible = textParagraphProperties.AlwaysCollapsible;
            _textWrap = textParagraphProperties.TextWrapping;
            _indent = textParagraphProperties.Indent;
        }



        /// <summary>
        /// This property specifies whether the primary text advance
        /// direction shall be left-to-right, right-to-left, or top-to-bottom.
        /// </summary>
        public override FlowDirection FlowDirection
        {
            get { return _flowDirection; }
        }


        /// <summary>
        /// This property describes how inline content of a block is aligned.
        /// </summary>
        public override TextAlignment TextAlignment
        {
            get { return _textAlignment; }
        }


        /// <summary>
        /// Paragraph's line height
        /// </summary>
        public override double LineHeight
        {
            get { return _lineHeight; }
        }


        /// <summary>
        /// Indicates the first line of the paragraph.
        /// </summary>
        public override bool FirstLineInParagraph
        {
            get { return _firstLineInParagraph; }
        }


        /// <summary>
        /// If true, the formatted line may always be collapsed. If false (the default),
        /// only lines that overflow the paragraph width are collapsed.
        /// </summary>
        public override bool AlwaysCollapsible
        {
            get { return _alwaysCollapsible; }
        }


        /// <summary>
        /// Paragraph's default run properties
        /// </summary>
        public override TextRunProperties DefaultTextRunProperties
        {
            get { return _defaultTextRunProperties; }
        }


        /// <summary>
        /// This property controls whether or not text wraps when it reaches the flow edge
        /// of its containing block box
        /// </summary>
        public override TextWrapping TextWrapping
        {
            get { return _textWrap; }
        }


        /// <summary>
        /// This property specifies marker characteristics of the first line in paragraph
        /// </summary>
        public override TextMarkerProperties TextMarkerProperties
        {
            get { return null; }
        }


        /// <summary>
        /// Line indentation
        /// </summary>
        public override double Indent
        {
            get { return _indent; }
        }

        /// <summary>
        /// Set text flow direction
        /// </summary>
        internal void SetFlowDirection(FlowDirection flowDirection)
        {
            _flowDirection = flowDirection;
        }


        /// <summary>
        /// Set text alignment
        /// </summary>
        internal void SetTextAlignment(TextAlignment textAlignment)
        {
            _textAlignment = textAlignment;
        }


        /// <summary>
        /// Set line height
        /// </summary>
        internal void SetLineHeight(double lineHeight)
        {
            _lineHeight = lineHeight;
        }

        /// <summary>
        /// Set text wrap
        /// </summary>
        internal void SetTextWrapping(TextWrapping textWrap)
        {
            _textWrap = textWrap;
        }

        private FlowDirection           _flowDirection;
        private TextAlignment           _textAlignment;
        private bool                    _firstLineInParagraph;
        private bool                    _alwaysCollapsible;
        private TextRunProperties       _defaultTextRunProperties;
        private TextWrapping            _textWrap;
        private double                  _indent;
        private double                  _lineHeight;
    }
}
