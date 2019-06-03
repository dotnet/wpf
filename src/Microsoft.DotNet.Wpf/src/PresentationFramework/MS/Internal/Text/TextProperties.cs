// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
// Description: Text run properties provider. 
//


using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;

namespace MS.Internal.Text
{
    // ----------------------------------------------------------------------
    // Text run properties provider.
    // ----------------------------------------------------------------------
    internal sealed class TextProperties : TextRunProperties
    {
        // ------------------------------------------------------------------
        //
        //  TextRunProperties Implementation
        //
        // ------------------------------------------------------------------

        #region TextRunProperties Implementation

        // ------------------------------------------------------------------
        // Typeface used to format and display text.
        // ------------------------------------------------------------------
        public override Typeface Typeface { get { return _typeface; }  }

        // ------------------------------------------------------------------
        // Em size of font used to format and display text.
        // ------------------------------------------------------------------
        public override double FontRenderingEmSize
        {
            get
            {
                double emSize = _fontSize;
                // Make sure that TextFormatter limitations are not exceeded.
                TextDpi.EnsureValidLineOffset(ref emSize);
                return emSize;
            }
        }

        // ------------------------------------------------------------------
        // Em size of font to determine subtle change in font hinting.
        // ------------------------------------------------------------------
        public override double FontHintingEmSize { get { return 12.0; } }

        // ------------------------------------------------------------------
        // Text decorations.
        // ------------------------------------------------------------------
        public override TextDecorationCollection TextDecorations { get { return _textDecorations; } }

        // ------------------------------------------------------------------
        // Text foreground bursh.
        // ------------------------------------------------------------------
        public override Brush ForegroundBrush { get { return _foreground; } }

        // ------------------------------------------------------------------
        // Text background brush.
        // ------------------------------------------------------------------
        public override Brush BackgroundBrush { get { return _backgroundBrush; } }

        // ------------------------------------------------------------------
        // Text vertical alignment.
        // ------------------------------------------------------------------
        public override BaselineAlignment BaselineAlignment { get { return _baselineAlignment; } }

        // ------------------------------------------------------------------
        // Text culture info.
        // ------------------------------------------------------------------
        public override CultureInfo CultureInfo { get { return _cultureInfo; } }

        // ------------------------------------------------------------------
        // Number substitution
        // ------------------------------------------------------------------
        public override NumberSubstitution NumberSubstitution { get { return _numberSubstitution; } }

        // ------------------------------------------------------------------
        // Typography properties
        // ------------------------------------------------------------------
        public override TextRunTypographyProperties TypographyProperties{ get { return _typographyProperties; } }

        // ------------------------------------------------------------------
        // TextEffects property
        // ------------------------------------------------------------------
        public override TextEffectCollection TextEffects { get { return _textEffects; } }

        #endregion TextRunProperties Implementation

        // ------------------------------------------------------------------
        // Constructor.
        // ------------------------------------------------------------------
        internal TextProperties(FrameworkElement target, bool isTypographyDefaultValue)
        {
            // if none of the number substitution properties have changed, initialize the
            // _numberSubstitution field to a known default value
            if (!target.HasNumberSubstitutionChanged)
            {
                _numberSubstitution = FrameworkElement.DefaultNumberSubstitution;
            }

            PixelsPerDip = target.GetDpi().PixelsPerDip;

            InitCommon(target);
            if (!isTypographyDefaultValue)
            {
                _typographyProperties = TextElement.GetTypographyProperties(target);
            }
            else
            {
                _typographyProperties = Typography.Default;
            }
            
            _baselineAlignment = BaselineAlignment.Baseline;
        }

        internal TextProperties(DependencyObject target, StaticTextPointer position, bool inlineObjects, bool getBackground, double pixelsPerDip)
        {
            // if none of the number substitution properties have changed, we may be able to
            // initialize the _numberSubstitution field to a known default value
            FrameworkContentElement fce = target as FrameworkContentElement;
            if (fce != null)
            {
                if (!fce.HasNumberSubstitutionChanged)
                {
                    _numberSubstitution = FrameworkContentElement.DefaultNumberSubstitution;
                }
            }
            else
            {
                FrameworkElement fe = target as FrameworkElement;
                if (fe != null && !fe.HasNumberSubstitutionChanged)
                {
                    _numberSubstitution = FrameworkElement.DefaultNumberSubstitution;
                }               
            }

            PixelsPerDip = pixelsPerDip;
            InitCommon(target);

            _typographyProperties = GetTypographyProperties(target);
            if (!inlineObjects)
            {
                _baselineAlignment = DynamicPropertyReader.GetBaselineAlignment(target);

                if (!position.IsNull)
                {
                    TextDecorationCollection highlightDecorations = GetHighlightTextDecorations(position);
                    if (highlightDecorations != null)
                    {
                        // Highlights (if present) take precedence over property value TextDecorations.
                        _textDecorations = highlightDecorations;
                    }
                }

                if (getBackground)
                {
                    _backgroundBrush = DynamicPropertyReader.GetBackgroundBrush(target);
                }
            }
            else
            {
                _baselineAlignment = DynamicPropertyReader.GetBaselineAlignmentForInlineObject(target);
                _textDecorations = DynamicPropertyReader.GetTextDecorationsForInlineObject(target, _textDecorations);

                if (getBackground)
                {
                    _backgroundBrush = DynamicPropertyReader.GetBackgroundBrushForInlineObject(position);
                }
            }
        }

        // Copy constructor, with override for default TextDecorationCollection value.
        internal TextProperties(TextProperties source, TextDecorationCollection textDecorations)
        {
            _backgroundBrush = source._backgroundBrush;
            _typeface = source._typeface;
            _fontSize = source._fontSize;
            _foreground = source._foreground;
            _textEffects = source._textEffects;
            _cultureInfo = source._cultureInfo;
            _numberSubstitution = source._numberSubstitution;
            _typographyProperties = source._typographyProperties;
            _baselineAlignment = source._baselineAlignment;
            PixelsPerDip = source.PixelsPerDip;
            _textDecorations = textDecorations;
        }

        // assigns values to all fields except for _typographyProperties, _baselineAlignment,
        // and _background, which are set appropriately in each constructor
        private void InitCommon(DependencyObject target)
        {
            _typeface = DynamicPropertyReader.GetTypeface(target);

            _fontSize = (double)target.GetValue(TextElement.FontSizeProperty);
            _foreground = (Brush) target.GetValue(TextElement.ForegroundProperty);
            _textEffects = DynamicPropertyReader.GetTextEffects(target);

            _cultureInfo = DynamicPropertyReader.GetCultureInfo(target);
            _textDecorations = DynamicPropertyReader.GetTextDecorations(target);

            // as an optimization, we may have already initialized _numberSubstitution to a default
            // value if none of the NumberSubstitution dependency properties have changed
            if (_numberSubstitution == null)
            {
                _numberSubstitution = DynamicPropertyReader.GetNumberSubstitution(target);
            }
        }

        // Gathers text decorations set on scoping highlights.
        // If no highlight properties are found, returns null
        private static TextDecorationCollection GetHighlightTextDecorations(StaticTextPointer highlightPosition)
        {
            TextDecorationCollection textDecorations = null;
            Highlights highlights = highlightPosition.TextContainer.Highlights;

            if (highlights == null)
            {
                return textDecorations;
            }

            //
            // Speller
            //
            textDecorations = highlights.GetHighlightValue(highlightPosition, LogicalDirection.Forward, typeof(SpellerHighlightLayer)) as TextDecorationCollection;

            //
            // IME composition
            //
#if UNUSED_IME_HIGHLIGHT_LAYER
            TextDecorationCollection imeTextDecorations = highlights.GetHighlightValue(highlightPosition, LogicalDirection.Forward, typeof(FrameworkTextComposition)) as TextDecorationCollection;
            if (imeTextDecorations != null)
            {
                textDecorations = imeTextDecorations;
            }
#endif

            return textDecorations;
        }

        // ------------------------------------------------------------------
        // Retrieve typography properties from specified element.
        // ------------------------------------------------------------------
        private static TypographyProperties GetTypographyProperties(DependencyObject element)
        {
            Debug.Assert(element != null);

            TextBlock tb = element as TextBlock;
            if (tb != null)
            {
                if(!tb.IsTypographyDefaultValue)
                {
                    return TextElement.GetTypographyProperties(element);
                }
                else
                {
                    return Typography.Default;
                }
            }

            TextBox textBox = element as TextBox;
            if (textBox != null)
            {
                if (!textBox.IsTypographyDefaultValue)
                {
                    return TextElement.GetTypographyProperties(element);
                }
                else
                {
                    return Typography.Default;
                }
            }

            TextElement te = element as TextElement;
            if (te != null)
            {
                return te.TypographyPropertiesGroup;
            }

            FlowDocument fd = element as FlowDocument;
            if (fd != null)
            {
               return fd.TypographyPropertiesGroup;
            }

            // return default typography properties group
            return Typography.Default;
        }

        /// <summary>
        /// Set the BackgroundBrush
        /// </summary>
        /// <param name="backgroundBrush">The brush to set to</param>
        internal void SetBackgroundBrush(Brush backgroundBrush)
        {
            _backgroundBrush = backgroundBrush;
        }

        /// <summary>
        /// Set the ForegroundBrush
        /// </summary>
        /// <param name="foregroundBrush">The brush to set to</param>
        internal void SetForegroundBrush(Brush foregroundBrush)
        {
            _foreground = foregroundBrush;
        }

        // ------------------------------------------------------------------
        // Typeface.
        // ------------------------------------------------------------------
        private Typeface _typeface;

        // ------------------------------------------------------------------
        // Font size.
        // ------------------------------------------------------------------
        private double _fontSize;

        // ------------------------------------------------------------------
        // Foreground brush.
        // ------------------------------------------------------------------
        private Brush _foreground;

        // ------------------------------------------------------------------
        // Text effects flags.
        // ------------------------------------------------------------------
        private TextEffectCollection _textEffects;

        // ------------------------------------------------------------------
        // Text decorations.
        // ------------------------------------------------------------------
        private TextDecorationCollection _textDecorations;

        // ------------------------------------------------------------------
        // Baseline alignment.
        // ------------------------------------------------------------------
        private BaselineAlignment _baselineAlignment;

        // ------------------------------------------------------------------
        // Text background brush.
        // ------------------------------------------------------------------
        private Brush _backgroundBrush;

        // ------------------------------------------------------------------
        // Culture info.
        // ------------------------------------------------------------------
        private CultureInfo _cultureInfo;

        // ------------------------------------------------------------------
        // Number Substitution
        // ------------------------------------------------------------------
        private NumberSubstitution _numberSubstitution;
        
        // ------------------------------------------------------------------
        // Typography properties group.
        // ------------------------------------------------------------------
        private TextRunTypographyProperties _typographyProperties;
    }
}
