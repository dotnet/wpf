// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: Implementation of FormattedText class. The FormattedText class is targeted at programmers
// needing to add some simple text to a MIL visual.
//
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using System.Runtime.InteropServices;
using MS.Internal;
using MS.Internal.TextFormatting;
using MS.Internal.FontFace;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

#pragma warning disable 1634, 1691
//Allow suppression of Presharp warnings

namespace System.Windows.Media
{
    /// <summary>
    /// The FormattedText class is targeted at programmers needing to add some simple text to a MIL visual.
    /// </summary>
    public class FormattedText
    {
        #region Construction

        /// <summary>
        /// Construct a FormattedText object.
        /// </summary>
        /// <param name="textToFormat">String of text to be displayed.</param>
        /// <param name="culture">Culture of text.</param>
        /// <param name="flowDirection">Flow direction of text.</param>
        /// <param name="typeface">Type face used to display text.</param>
        /// <param name="emSize">Font em size in visual units (1/96 of an inch).</param>
        /// <param name="foreground">Foreground brush used to render text.</param>
        [Obsolete("Use the PixelsPerDip override", false)]
        public FormattedText(
            string textToFormat,
            CultureInfo culture,
            FlowDirection flowDirection,
            Typeface typeface,
            double emSize,
            Brush foreground) : this(
                textToFormat,
                culture,
                flowDirection,
                typeface,
                emSize,
                foreground,
                null,
                TextFormattingMode.Ideal
                )

        {
        }

        /// <summary>
        /// Construct a FormattedText object.
        /// </summary>
        /// <param name="textToFormat">String of text to be displayed.</param>
        /// <param name="culture">Culture of text.</param>
        /// <param name="flowDirection">Flow direction of text.</param>
        /// <param name="typeface">Type face used to display text.</param>
        /// <param name="emSize">Font em size in visual units (1/96 of an inch).</param>
        /// <param name="foreground">Foreground brush used to render text.</param>
        /// <param name="pixelsPerDip">DPI scale on which to render text</param>
        public FormattedText(
            string textToFormat,
            CultureInfo culture,
            FlowDirection flowDirection,
            Typeface typeface,
            double emSize,
            Brush foreground,
            double pixelsPerDip) : this(
                textToFormat,
                culture,
                flowDirection,
                typeface,
                emSize,
                foreground,
                null,
                TextFormattingMode.Ideal,
                pixelsPerDip
                )

        {
        }

        /// <summary>
        /// Construct a FormattedText object.
        /// </summary>
        /// <param name="textToFormat">String of text to be displayed.</param>
        /// <param name="culture">Culture of text.</param>
        /// <param name="flowDirection">Flow direction of text.</param>
        /// <param name="typeface">Type face used to display text.</param>
        /// <param name="emSize">Font em size in visual units (1/96 of an inch).</param>
        /// <param name="foreground">Foreground brush used to render text.</param>
        /// <param name="numberSubstitution">Number substitution behavior to apply to the text; can be null,
        /// in which case the default number number method for the text culture is used.</param>
        [Obsolete("Use the PixelsPerDip override", false)]
        public FormattedText(
            string textToFormat,
            CultureInfo culture,
            FlowDirection flowDirection,
            Typeface typeface,
            double emSize,
            Brush foreground,
            NumberSubstitution numberSubstitution) : this(
                textToFormat,
                culture,
                flowDirection,
                typeface,
                emSize,
                foreground,
                numberSubstitution,
                TextFormattingMode.Ideal
                )
        {
        }

        /// <summary>
        /// Construct a FormattedText object.
        /// </summary>
        /// <param name="textToFormat">String of text to be displayed.</param>
        /// <param name="culture">Culture of text.</param>
        /// <param name="flowDirection">Flow direction of text.</param>
        /// <param name="typeface">Type face used to display text.</param>
        /// <param name="emSize">Font em size in visual units (1/96 of an inch).</param>
        /// <param name="foreground">Foreground brush used to render text.</param>
        /// <param name="numberSubstitution">Number substitution behavior to apply to the text; can be null,
        /// in which case the default number number method for the text culture is used.</param>
        /// <param name="pixelsPerDip">DPI scale on which to render text.</param>
        public FormattedText(
            string textToFormat,
            CultureInfo culture,
            FlowDirection flowDirection,
            Typeface typeface,
            double emSize,
            Brush foreground,
            NumberSubstitution numberSubstitution,
            double pixelsPerDip) : this(
                textToFormat,
                culture,
                flowDirection,
                typeface,
                emSize,
                foreground,
                numberSubstitution,
                TextFormattingMode.Ideal,
                pixelsPerDip
                )
        {
        }

        /// <summary>
        /// Construct a FormattedText object.
        /// </summary>
        /// <param name="textToFormat">String of text to be displayed.</param>
        /// <param name="culture">Culture of text.</param>
        /// <param name="flowDirection">Flow direction of text.</param>
        /// <param name="typeface">Type face used to display text.</param>
        /// <param name="emSize">Font em size in visual units (1/96 of an inch).</param>
        /// <param name="foreground">Foreground brush used to render text.</param>
        /// <param name="numberSubstitution">Number substitution behavior to apply to the text; can be null,
        /// in which case the default number number method for the text culture is used.</param>
        [Obsolete("Use the PixelsPerDip override", false)]
        public FormattedText(
            string textToFormat,
            CultureInfo culture,
            FlowDirection flowDirection,
            Typeface typeface,
            double emSize,
            Brush foreground,
            NumberSubstitution numberSubstitution,
            TextFormattingMode textFormattingMode)
        {
            InitFormattedText(textToFormat, culture, flowDirection, typeface, emSize, foreground, numberSubstitution, textFormattingMode, _pixelsPerDip);
        }

        /// <summary>
        /// Construct a FormattedText object.
        /// </summary>
        /// <param name="textToFormat">String of text to be displayed.</param>
        /// <param name="culture">Culture of text.</param>
        /// <param name="flowDirection">Flow direction of text.</param>
        /// <param name="typeface">Type face used to display text.</param>
        /// <param name="emSize">Font em size in visual units (1/96 of an inch).</param>
        /// <param name="foreground">Foreground brush used to render text.</param>
        /// <param name="numberSubstitution">Number substitution behavior to apply to the text; can be null,
        /// in which case the default number number method for the text culture is used.</param>
        /// <param name="pixelsPerDip">DPI scale on which to render text.</param>
        public FormattedText(
            string textToFormat,
            CultureInfo culture,
            FlowDirection flowDirection,
            Typeface typeface,
            double emSize,
            Brush foreground,
            NumberSubstitution numberSubstitution,
            TextFormattingMode textFormattingMode,
            double pixelsPerDip)
        {
            InitFormattedText(textToFormat, culture, flowDirection, typeface, emSize, foreground, numberSubstitution, textFormattingMode, pixelsPerDip);
        }

        private void InitFormattedText(string textToFormat, CultureInfo culture, FlowDirection flowDirection, Typeface typeface,
            double emSize, Brush foreground, NumberSubstitution numberSubstitution, TextFormattingMode textFormattingMode, double pixelsPerDip)
        {
            if (textToFormat == null)
                throw new ArgumentNullException("textToFormat");

            if (typeface == null)
                throw new ArgumentNullException("typeface");

            ValidateCulture(culture);
            ValidateFlowDirection(flowDirection, "flowDirection");
            ValidateFontSize(emSize);
            _pixelsPerDip = pixelsPerDip;

            _textFormattingMode = textFormattingMode;
            _text = textToFormat;
            GenericTextRunProperties runProps = new GenericTextRunProperties(
                typeface,
                emSize,
                12.0f, // default hinting size
                _pixelsPerDip,
                null, // decorations
                foreground,
                null, // highlight background
                BaselineAlignment.Baseline,
                culture,
                numberSubstitution
                );
            _latestPosition = _formatRuns.SetValue(0, _text.Length, runProps, _latestPosition);

            _defaultParaProps = new GenericTextParagraphProperties(
                flowDirection,
                TextAlignment.Left,
                false,
                false,
                runProps,
                TextWrapping.WrapWithOverflow,
                0, // line height not specified
                0 // indentation not specified
                );

            InvalidateMetrics();
        }

        /// <summary>
        /// Returns the string of text to be displayed
        /// </summary>
        public string Text
        {
            get { return _text; }
        }

        /// <summary>
        /// Sets the PixelsPerDip at which this text should be rendered. Must be set when creating FormattedObject and updated when DPI changes.
        /// </summary>
        public double PixelsPerDip
        {
            get { return _pixelsPerDip; }
            set
            {
                _pixelsPerDip = value;
                _defaultParaProps.DefaultTextRunProperties.PixelsPerDip = _pixelsPerDip;
            }
        }
    
    #endregion

        #region Formatting properties

        private static void ValidateCulture(CultureInfo culture)
        {
            if (culture == null)
                throw new ArgumentNullException("culture");
        }

        private static void ValidateFontSize(double emSize)
        {
            if (emSize <= 0)
                throw new ArgumentOutOfRangeException("emSize", SR.Get(SRID.ParameterMustBeGreaterThanZero));

            if (emSize > MaxFontEmSize)
                throw new ArgumentOutOfRangeException("emSize", SR.Get(SRID.ParameterCannotBeGreaterThan, MaxFontEmSize));

            if (DoubleUtil.IsNaN(emSize))
                throw new ArgumentOutOfRangeException("emSize", SR.Get(SRID.ParameterValueCannotBeNaN));
        }

        private static void ValidateFlowDirection(FlowDirection flowDirection, string parameterName)
        {
            if ((int)flowDirection < 0 || (int)flowDirection > (int)FlowDirection.RightToLeft)
                throw new InvalidEnumArgumentException(parameterName, (int)flowDirection, typeof(FlowDirection));
        }

        private int ValidateRange(int startIndex, int count)
        {
            if (startIndex < 0 || startIndex > _text.Length)
                throw new ArgumentOutOfRangeException("startIndex");

            int limit = startIndex + count;

            if (count < 0 || limit < startIndex || limit > _text.Length)
                throw new ArgumentOutOfRangeException("count");

            return limit;
        }

        private void InvalidateMetrics()
        {
            _metrics = null;
            _minWidth = double.MinValue;
        }

        /// <summary>
        /// Sets foreground brush used for drawing text
        /// </summary>
        /// <param name="foregroundBrush">Foreground brush</param>
        public void SetForegroundBrush(Brush foregroundBrush)
        {
            SetForegroundBrush(foregroundBrush, 0, _text.Length);
        }

        /// <summary>
        /// Sets foreground brush used for drawing text
        /// </summary>
        /// <param name="foregroundBrush">Foreground brush</param>
        /// <param name="startIndex">The start index of initial character to apply the change to.</param>
        /// <param name="count">The number of characters the change should be applied to.</param>
        public void SetForegroundBrush(Brush foregroundBrush, int startIndex, int count)
        {
            int limit = ValidateRange(startIndex, count);
            for (int i = startIndex; i < limit;)
            {
                SpanRider formatRider = new SpanRider(_formatRuns, _latestPosition, i);
                i = Math.Min(limit, i + formatRider.Length);

#pragma warning disable 6506
                // Presharp warns that runProps is not validated, but it can never be null 
                // because the rider is already checked to be in range
                GenericTextRunProperties runProps = formatRider.CurrentElement as GenericTextRunProperties;
                
                Invariant.Assert(runProps != null);
                
                if (runProps.ForegroundBrush == foregroundBrush)
                    continue;
                    
                GenericTextRunProperties newProps = new GenericTextRunProperties(
                    runProps.Typeface,
                    runProps.FontRenderingEmSize,
                    runProps.FontHintingEmSize,
                    _pixelsPerDip,
                    runProps.TextDecorations,
                    foregroundBrush,
                    runProps.BackgroundBrush,
                    runProps.BaselineAlignment,
                    runProps.CultureInfo,
                    runProps.NumberSubstitution
                    );
#pragma warning restore 6506
                _latestPosition = _formatRuns.SetValue(formatRider.CurrentPosition, i - formatRider.CurrentPosition, newProps, formatRider.SpanPosition);
            }
        }

        /// <summary>
        /// Sets or changes the font family for the text object 
        /// </summary>
        /// <param name="fontFamily">Font family name</param>
        public void SetFontFamily(string fontFamily)
        {
            SetFontFamily(fontFamily, 0, _text.Length);
        }

        /// <summary>
        /// Sets or changes the font family for the text object 
        /// </summary>
        /// <param name="fontFamily">Font family name</param>
        /// <param name="startIndex">The start index of initial character to apply the change to.</param>
        /// <param name="count">The number of characters the change should be applied to.</param>
        public void SetFontFamily(string fontFamily, int startIndex, int count)
        {
            if (fontFamily == null)
                throw new ArgumentNullException("fontFamily");

            SetFontFamily(new FontFamily(fontFamily), startIndex, count);
        }

        /// <summary>
        /// Sets or changes the font family for the text object 
        /// </summary>
        /// <param name="fontFamily">Font family</param>
        public void SetFontFamily(FontFamily fontFamily)
        {
            SetFontFamily(fontFamily, 0, _text.Length);
        }

        /// <summary>
        /// Sets or changes the font family for the text object 
        /// </summary>
        /// <param name="fontFamily">Font family</param>
        /// <param name="startIndex">The start index of initial character to apply the change to.</param>
        /// <param name="count">The number of characters the change should be applied to.</param>
        public void SetFontFamily(FontFamily fontFamily, int startIndex, int count)
        {
            if (fontFamily == null)
                throw new ArgumentNullException("fontFamily");

            int limit = ValidateRange(startIndex, count);
            for (int i = startIndex; i < limit;)
            {
                SpanRider formatRider = new SpanRider(_formatRuns, _latestPosition, i);
                i = Math.Min(limit, i + formatRider.Length);

#pragma warning disable 6506
                // Presharp warns that runProps is not validated, but it can never be null 
                // because the rider is already checked to be in range
                GenericTextRunProperties runProps = formatRider.CurrentElement as GenericTextRunProperties;
                
                Invariant.Assert(runProps != null);
                
                Typeface oldTypeface = runProps.Typeface;
                if (fontFamily.Equals(oldTypeface.FontFamily))
                    continue;

                GenericTextRunProperties newProps = new GenericTextRunProperties(
                    new Typeface(fontFamily, oldTypeface.Style, oldTypeface.Weight, oldTypeface.Stretch),
                    runProps.FontRenderingEmSize,
                    runProps.FontHintingEmSize,
                    _pixelsPerDip,
                    runProps.TextDecorations,
                    runProps.ForegroundBrush,
                    runProps.BackgroundBrush,
                    runProps.BaselineAlignment,
                    runProps.CultureInfo,
                    runProps.NumberSubstitution
                    );
#pragma warning restore 6506
                _latestPosition = _formatRuns.SetValue(formatRider.CurrentPosition, i - formatRider.CurrentPosition, newProps, formatRider.SpanPosition);
                InvalidateMetrics();
            }
        }


        /// <summary>
        /// Sets or changes the font em size measured in MIL units
        /// </summary>
        /// <param name="emSize">Font em size</param>
        public void SetFontSize(double emSize)
        {
            SetFontSize(emSize, 0, _text.Length);
        }

        /// <summary>
        /// Sets or changes the font em size measured in MIL units
        /// </summary>
        /// <param name="emSize">Font em size</param>
        /// <param name="startIndex">The start index of initial character to apply the change to.</param>
        /// <param name="count">The number of characters the change should be applied to.</param>
        public void SetFontSize(double emSize, int startIndex, int count)
        {
            ValidateFontSize(emSize);

            int limit = ValidateRange(startIndex, count);
            for (int i = startIndex; i < limit;)
            {
                SpanRider formatRider = new SpanRider(_formatRuns, _latestPosition, i);
                i = Math.Min(limit, i + formatRider.Length);

#pragma warning disable 6506
                // Presharp warns that runProps is not validated, but it can never be null 
                // because the rider is already checked to be in range
                GenericTextRunProperties runProps = formatRider.CurrentElement as GenericTextRunProperties;
                
                Invariant.Assert(runProps != null);
                
                if (runProps.FontRenderingEmSize == emSize)
                    continue;

                GenericTextRunProperties newProps = new GenericTextRunProperties(
                    runProps.Typeface,
                    emSize,
                    runProps.FontHintingEmSize,
                    _pixelsPerDip,
                    runProps.TextDecorations,
                    runProps.ForegroundBrush,
                    runProps.BackgroundBrush,
                    runProps.BaselineAlignment,
                    runProps.CultureInfo,
                    runProps.NumberSubstitution
                    );
                _latestPosition = _formatRuns.SetValue(formatRider.CurrentPosition, i - formatRider.CurrentPosition, newProps, formatRider.SpanPosition);
#pragma warning restore 6506
                InvalidateMetrics();
            }
        }

        /// <summary>
        /// Sets or changes the culture for the text object.
        /// </summary>
        /// <param name="culture">The new culture for the text object.</param>
        public void SetCulture(CultureInfo culture)
        {
            SetCulture(culture, 0, _text.Length);
        }

        /// <summary>
        /// Sets or changes the culture for the text object.
        /// </summary>
        /// <param name="culture">The new culture for the text object.</param>
        /// <param name="startIndex">The start index of initial character to apply the change to.</param>
        /// <param name="count">The number of characters the change should be applied to.</param>
        public void SetCulture(CultureInfo culture, int startIndex, int count)
        {
            ValidateCulture(culture);

            int limit = ValidateRange(startIndex, count);
            for (int i = startIndex; i < limit; )
            {
                SpanRider formatRider = new SpanRider(_formatRuns, _latestPosition, i);
                i = Math.Min(limit, i + formatRider.Length);

#pragma warning disable 6506 
                // Presharp warns that runProps is not validated, but it can never be null 
                // because the rider is already checked to be in range
                GenericTextRunProperties runProps = formatRider.CurrentElement as GenericTextRunProperties;
                
                Invariant.Assert(runProps != null);
                
                if (runProps.CultureInfo.Equals(culture))
                    continue;

                GenericTextRunProperties newProps = new GenericTextRunProperties(
                    runProps.Typeface,
                    runProps.FontRenderingEmSize,
                    runProps.FontHintingEmSize,
                    _pixelsPerDip,
                    runProps.TextDecorations,
                    runProps.ForegroundBrush,
                    runProps.BackgroundBrush,
                    runProps.BaselineAlignment,
                    culture,
                    runProps.NumberSubstitution
                    );
#pragma warning restore 6506
                _latestPosition = _formatRuns.SetValue(formatRider.CurrentPosition, i - formatRider.CurrentPosition, newProps, formatRider.SpanPosition);
                InvalidateMetrics();
            }
        }

        /// <summary>
        /// Sets or changes the number substitution behavior for the text.
        /// </summary>
        /// <param name="numberSubstitution">Number substitution behavior to apply to the text; can be null,
        /// in which case the default number substitution method for the text culture is used.</param>
        public void SetNumberSubstitution(
            NumberSubstitution numberSubstitution
            )
        {
            SetNumberSubstitution(numberSubstitution, 0, _text.Length);
        }

        /// <summary>
        /// Sets or changes the number substitution behavior for a range of text.
        /// </summary>
        /// <param name="numberSubstitution">Number substitution behavior to apply to the text; can be null,
        /// in which case the default number substitution method for the text culture is used.</param>
        /// <param name="startIndex">The start index of initial character to apply the change to.</param>
        /// <param name="count">The number of characters the change should be applied to.</param>
        public void SetNumberSubstitution(
            NumberSubstitution numberSubstitution,
            int startIndex,
            int count
            )
        {
            int limit = ValidateRange(startIndex, count);
            for (int i = startIndex; i < limit; )
            {
                SpanRider formatRider = new SpanRider(_formatRuns, _latestPosition, i);
                i = Math.Min(limit, i + formatRider.Length);

#pragma warning disable 6506
                // Presharp warns that runProps is not validated, but it can never be null 
                // because the rider is already checked to be in range
                GenericTextRunProperties runProps = formatRider.CurrentElement as GenericTextRunProperties;

                Invariant.Assert(runProps != null);

                if (numberSubstitution != null)
                {
                    if (numberSubstitution.Equals(runProps.NumberSubstitution))
                        continue;
                }
                else
                {
                    if (runProps.NumberSubstitution == null)
                        continue;
                }

                GenericTextRunProperties newProps = new GenericTextRunProperties(
                    runProps.Typeface,
                    runProps.FontRenderingEmSize,
                    runProps.FontHintingEmSize,
                    _pixelsPerDip,
                    runProps.TextDecorations,
                    runProps.ForegroundBrush,
                    runProps.BackgroundBrush,
                    runProps.BaselineAlignment,
                    runProps.CultureInfo,
                    numberSubstitution
                    );
#pragma warning restore 6506
                _latestPosition = _formatRuns.SetValue(formatRider.CurrentPosition, i - formatRider.CurrentPosition, newProps, formatRider.SpanPosition);
                InvalidateMetrics();
            }
        }

        /// <summary>
        /// Sets or changes the font weight
        /// </summary>
        /// <param name="weight">Font weight</param>
        public void SetFontWeight(FontWeight weight)
        {
            SetFontWeight(weight, 0, _text.Length);
        }

        /// <summary>
        /// Sets or changes the font weight
        /// </summary>
        /// <param name="weight">Font weight</param>
        /// <param name="startIndex">The start index of initial character to apply the change to.</param>
        /// <param name="count">The number of characters the change should be applied to.</param>
        public void SetFontWeight(FontWeight weight, int startIndex, int count)
        {
            int limit = ValidateRange(startIndex, count);
            for (int i = startIndex; i < limit;)
            {
                SpanRider formatRider = new SpanRider(_formatRuns, _latestPosition, i);
                i = Math.Min(limit, i + formatRider.Length);

#pragma warning disable 6506 
                // Presharp warns that runProps is not validated, but it can never be null 
                // because the rider is already checked to be in range
                GenericTextRunProperties runProps = formatRider.CurrentElement as GenericTextRunProperties;
                
                Invariant.Assert(runProps != null);
                
                Typeface oldTypeface = runProps.Typeface;
                if (oldTypeface.Weight == weight)
                    continue;

                GenericTextRunProperties newProps = new GenericTextRunProperties(
                    new Typeface(oldTypeface.FontFamily, oldTypeface.Style, weight, oldTypeface.Stretch),
                    runProps.FontRenderingEmSize,
                    runProps.FontHintingEmSize,
                    _pixelsPerDip,
                    runProps.TextDecorations,
                    runProps.ForegroundBrush,
                    runProps.BackgroundBrush,
                    runProps.BaselineAlignment,
                    runProps.CultureInfo,
                    runProps.NumberSubstitution
                    );
#pragma warning restore 6506 
                _latestPosition = _formatRuns.SetValue(formatRider.CurrentPosition, i - formatRider.CurrentPosition, newProps, formatRider.SpanPosition);
                InvalidateMetrics();
            }
        }

        /// <summary>
        /// Sets or changes the font style
        /// </summary>
        /// <param name="style">Font style</param>
        public void SetFontStyle(FontStyle style)
        {
            SetFontStyle(style, 0, _text.Length);
        }

        /// <summary>
        /// Sets or changes the font style
        /// </summary>
        /// <param name="style">Font style</param>
        /// <param name="startIndex">The start index of initial character to apply the change to.</param>
        /// <param name="count">The number of characters the change should be applied to.</param>
        public void SetFontStyle(FontStyle style, int startIndex, int count)
        {
            int limit = ValidateRange(startIndex, count);
            for (int i = startIndex; i < limit;)
            {
                SpanRider formatRider = new SpanRider(_formatRuns, _latestPosition, i);
                i = Math.Min(limit, i + formatRider.Length);

#pragma warning disable 6506 
                // Presharp warns that runProps is not validated, but it can never be null 
                // because the rider is already checked to be in range
                GenericTextRunProperties runProps = formatRider.CurrentElement as GenericTextRunProperties;
                
                Invariant.Assert(runProps != null);
                
                Typeface oldTypeface = runProps.Typeface;
                if (oldTypeface.Style == style)
                    continue;

                GenericTextRunProperties newProps = new GenericTextRunProperties(
                    new Typeface(oldTypeface.FontFamily, style, oldTypeface.Weight, oldTypeface.Stretch),
                    runProps.FontRenderingEmSize,
                    runProps.FontHintingEmSize,
                    _pixelsPerDip,
                    runProps.TextDecorations,
                    runProps.ForegroundBrush,
                    runProps.BackgroundBrush,
                    runProps.BaselineAlignment,
                    runProps.CultureInfo,
                    runProps.NumberSubstitution
                    );
#pragma warning restore 6506 
                
                _latestPosition = _formatRuns.SetValue(formatRider.CurrentPosition, i - formatRider.CurrentPosition, newProps, formatRider.SpanPosition);
                InvalidateMetrics(); // invalidate cached metrics
            }
        }

        /// <summary>
        /// Sets or changes the font stretch
        /// </summary>
        /// <param name="stretch">Font stretch</param>
        public void SetFontStretch(FontStretch stretch)
        {
            SetFontStretch(stretch, 0, _text.Length);
        }

        /// <summary>
        /// Sets or changes the font stretch
        /// </summary>
        /// <param name="stretch">Font stretch</param>
        /// <param name="startIndex">The start index of initial character to apply the change to.</param>
        /// <param name="count">The number of characters the change should be applied to.</param>
        public void SetFontStretch(FontStretch stretch, int startIndex, int count)
        {
            int limit = ValidateRange(startIndex, count);
            for (int i = startIndex; i < limit;)
            {
                SpanRider formatRider = new SpanRider(_formatRuns, _latestPosition, i);
                i = Math.Min(limit, i + formatRider.Length);

#pragma warning disable 6506 
                // Presharp warns that runProps is not validated, but it can never be null 
                // because the rider is already checked to be in range
                GenericTextRunProperties runProps = formatRider.CurrentElement as GenericTextRunProperties;
                
                Invariant.Assert(runProps != null);
                
                Typeface oldTypeface = runProps.Typeface;
                if (oldTypeface.Stretch == stretch)
                    continue;

                GenericTextRunProperties newProps = new GenericTextRunProperties(
                    new Typeface(oldTypeface.FontFamily, oldTypeface.Style, oldTypeface.Weight, stretch),
                    runProps.FontRenderingEmSize,
                    runProps.FontHintingEmSize,
                    _pixelsPerDip,
                    runProps.TextDecorations,
                    runProps.ForegroundBrush,
                    runProps.BackgroundBrush,
                    runProps.BaselineAlignment,
                    runProps.CultureInfo,
                    runProps.NumberSubstitution
                    );
                _latestPosition = _formatRuns.SetValue(formatRider.CurrentPosition, i - formatRider.CurrentPosition, newProps, formatRider.SpanPosition);
#pragma warning restore 6506 
                
                InvalidateMetrics();
            }
        }

        /// <summary>
        /// Sets or changes the type face
        /// </summary>
        /// <param name="typeface">Typeface</param>
        public void SetFontTypeface(Typeface typeface)
        {
            SetFontTypeface(typeface, 0, _text.Length);
        }

        /// <summary>
        /// Sets or changes the type face
        /// </summary>
        /// <param name="typeface">Typeface</param>
        /// <param name="startIndex">The start index of initial character to apply the change to.</param>
        /// <param name="count">The number of characters the change should be applied to.</param>
        public void SetFontTypeface(Typeface typeface, int startIndex, int count)
        {
            int limit = ValidateRange(startIndex, count);
            for (int i = startIndex; i < limit;)
            {
                SpanRider formatRider = new SpanRider(_formatRuns, _latestPosition, i);
                i = Math.Min(limit, i + formatRider.Length);

#pragma warning disable 6506 
                // Presharp warns that runProps is not validated, but it can never be null 
                // because the rider is already checked to be in range
                GenericTextRunProperties runProps = formatRider.CurrentElement as GenericTextRunProperties;
                
                Invariant.Assert(runProps != null);
                
                if (runProps.Typeface == typeface)
                    continue;

                GenericTextRunProperties newProps = new GenericTextRunProperties(
                    typeface,
                    runProps.FontRenderingEmSize,
                    runProps.FontHintingEmSize,
                    _pixelsPerDip,
                    runProps.TextDecorations,
                    runProps.ForegroundBrush,
                    runProps.BackgroundBrush,
                    runProps.BaselineAlignment,
                    runProps.CultureInfo,
                    runProps.NumberSubstitution
                    );
#pragma warning restore 6506 
                
                _latestPosition = _formatRuns.SetValue(formatRider.CurrentPosition, i - formatRider.CurrentPosition, newProps, formatRider.SpanPosition);
                InvalidateMetrics();
            }
        }

        /// <summary>
        /// Sets or changes the text decorations
        /// </summary>
        /// <param name="textDecorations">Text decorations</param>
        public void SetTextDecorations(TextDecorationCollection textDecorations)
        {
            SetTextDecorations(textDecorations, 0, _text.Length);
        }

        /// <summary>
        /// Sets or changes the text decorations
        /// </summary>
        /// <param name="textDecorations">Text decorations</param>
        /// <param name="startIndex">The start index of initial character to apply the change to.</param>
        /// <param name="count">The number of characters the change should be applied to.</param>
        public void SetTextDecorations(TextDecorationCollection textDecorations, int startIndex, int count)
        {
            int limit = ValidateRange(startIndex, count);
            for (int i = startIndex; i < limit;)
            {
                SpanRider formatRider = new SpanRider(_formatRuns, _latestPosition, i);
                i = Math.Min(limit, i + formatRider.Length);

#pragma warning disable 6506 
                // Presharp warns that runProps is not validated, but it can never be null 
                // because the rider is already checked to be in range
                GenericTextRunProperties runProps = formatRider.CurrentElement as GenericTextRunProperties;

                Invariant.Assert(runProps != null);
                
                if (runProps.TextDecorations == textDecorations)
                    continue;

                GenericTextRunProperties newProps = new GenericTextRunProperties(
                    runProps.Typeface,
                    runProps.FontRenderingEmSize,
                    runProps.FontHintingEmSize,
                    _pixelsPerDip,
                    textDecorations,
                    runProps.ForegroundBrush,
                    runProps.BackgroundBrush,
                    runProps.BaselineAlignment,
                    runProps.CultureInfo,
                    runProps.NumberSubstitution
                    );
#pragma warning restore 6506 
                
                _latestPosition = _formatRuns.SetValue(formatRider.CurrentPosition, i - formatRider.CurrentPosition, newProps, formatRider.SpanPosition);
            }
        }

        #endregion

        #region Line enumerator
        /// Note: enumeration is temporarily made private
        /// because of PS #828532
        /// 
        /// <summary>
        /// Strongly typed enumerator used for enumerating text lines
        /// </summary>
        private struct LineEnumerator : IEnumerator, IDisposable
        {
            int             _textStorePosition;
            int             _lineCount;
            double          _totalHeight;
            TextLine        _currentLine;
            TextLine        _nextLine;
            TextFormatter   _formatter;
            FormattedText   _that;

            // these are needed because _currentLine can be disposed before the next MoveNext() call
            double          _previousHeight;
            int             _previousLength;

            // line break before _currentLine, needed in case we have to reformat it with collapsing symbol
            TextLineBreak       _previousLineBreak;

            internal LineEnumerator(FormattedText text)
            {
                _previousHeight = 0;
                _previousLength = 0;
                _previousLineBreak = null;

                _textStorePosition = 0;
                _lineCount = 0;
                _totalHeight = 0;
                _currentLine = null;
                _nextLine = null;
                _formatter = TextFormatter.FromCurrentDispatcher(text._textFormattingMode);
                _that = text;
                if (_that._textSourceImpl == null)
                    _that._textSourceImpl = new TextSourceImplementation(_that);
            }

            public void Dispose()
            {
                if (_currentLine != null)
                {
                    _currentLine.Dispose();
                    _currentLine = null;
                }

                if (_nextLine != null)
                {
                    _nextLine.Dispose();
                    _nextLine = null;
                }
            }

            internal int Position
            {
                get
                {
                    return _textStorePosition;
                }
            }

            internal int Length
            {
                get
                {
                    return _previousLength;
                }
            }

            /// <summary>
            /// Gets the current text line in the collection
            /// </summary>
            public TextLine Current
            {
                get
                {
                    return _currentLine;
                }
            }

            /// <summary>
            /// Gets the current text line in the collection
            /// </summary>
            object IEnumerator.Current
            {
                get
                {
                    return (Current);
                }
            }

            /// <summary>
            /// Gets the paragraph width used to format the current text line
            /// </summary>
            internal double CurrentParagraphWidth
            {
                get
                {
                    return MaxLineLength(_lineCount);
                }
            }

            private double MaxLineLength(int line)
            {
                if (_that._maxTextWidths == null)
                    return _that._maxTextWidth;
                return _that._maxTextWidths[Math.Min(line, _that._maxTextWidths.Length - 1)];
            }

            /// <summary>
            /// Advances the enumerator to the next text line of the collection
            /// </summary>
            /// <returns>true if the enumerator was successfully advanced to the next element;
            /// false if the enumerator has passed the end of the collection</returns>
            public bool MoveNext()
            {
                if (_currentLine == null)
                {   // this is the first line
                    if (_that._text.Length == 0)
                        return false;

                    _currentLine = FormatLine(
                        _that._textSourceImpl,
                        _textStorePosition,
                        MaxLineLength(_lineCount),
                        _that._defaultParaProps,
                        null // no previous line break
                        );

                    // check if this line fits the text height
                    if (_totalHeight + _currentLine.Height > _that._maxTextHeight)
                    {
                        _currentLine.Dispose();
                        _currentLine = null;
                        return false;
                    }
                    Debug.Assert(_nextLine == null);
                }
                else
                {
                    // there is no next line or it didn't fit
                    // either way we're finished
                    if (_nextLine == null)
                        return false;

                    _totalHeight += _previousHeight;
                    _textStorePosition += _previousLength;
                    ++_lineCount;

                    _currentLine = _nextLine;
                    _nextLine = null;
                }

                TextLineBreak currentLineBreak = _currentLine.GetTextLineBreak();

                // this line is guaranteed to fit the text height
                Debug.Assert(_totalHeight + _currentLine.Height <= _that._maxTextHeight);

                // now, check if the next line fits, we need to do this on this iteration
                // because we might need to add ellipsis to the current line
                // as a result of the next line measurement

                // maybe there is no next line at all
                if (_textStorePosition + _currentLine.Length < _that._text.Length)
                {
                    bool nextLineFits;

                    if (_lineCount + 1 >= _that._maxLineCount)
                        nextLineFits = false;
                    else
                    {
                        _nextLine = FormatLine(
                            _that._textSourceImpl,
                            _textStorePosition + _currentLine.Length,
                            MaxLineLength(_lineCount + 1),
                            _that._defaultParaProps,
                            currentLineBreak
                            );
                        nextLineFits = (_totalHeight + _currentLine.Height + _nextLine.Height <= _that._maxTextHeight);
                    }                       

                    if (!nextLineFits)
                    {
                        // next line doesn't fit
                        if (_nextLine != null)
                        {
                            _nextLine.Dispose();
                            _nextLine = null;
                        }

                        if (_that._trimming != TextTrimming.None && !_currentLine.HasCollapsed)
                        {
                            // recreate the current line with ellipsis added
                            // Note: Paragraph ellipsis is not supported today. We'll workaround
                            // it here by faking a non-wrap text on finite column width.
                            TextWrapping currentWrap = _that._defaultParaProps.TextWrapping;
                            _that._defaultParaProps.SetTextWrapping(TextWrapping.NoWrap);

                            if (currentLineBreak != null)
                                currentLineBreak.Dispose();

                            _currentLine.Dispose();
                            _currentLine = FormatLine(
                                _that._textSourceImpl,
                                _textStorePosition,
                                MaxLineLength(_lineCount),
                                _that._defaultParaProps,
                                _previousLineBreak
                                );

                            currentLineBreak = _currentLine.GetTextLineBreak();
                            _that._defaultParaProps.SetTextWrapping(currentWrap);
                        }
                    }
                }
                _previousHeight = _currentLine.Height;
                _previousLength = _currentLine.Length;

                if (_previousLineBreak != null)
                    _previousLineBreak.Dispose();

                _previousLineBreak = currentLineBreak;

                return true;
            }


            /// <summary>
            /// Wrapper of TextFormatter.FormatLine that auto-collapses the line if needed.
            /// </summary>
            private TextLine FormatLine(TextSource textSource, int textSourcePosition, double maxLineLength, TextParagraphProperties paraProps, TextLineBreak lineBreak)
            {
                TextLine line = _formatter.FormatLine(
                    textSource,
                    textSourcePosition,
                    maxLineLength,
                    paraProps,
                    lineBreak
                    );

                if (_that._trimming != TextTrimming.None && line.HasOverflowed && line.Length > 0)
                {
                    // what I really need here is the last displayed text run of the line
                    // textSourcePosition + line.Length - 1 works except the end of paragraph case,
                    // where line length includes the fake paragraph break run
                    Debug.Assert(_that._text.Length > 0 && textSourcePosition + line.Length <= _that._text.Length + 1);

                    SpanRider thatFormatRider = new SpanRider(
                        _that._formatRuns,
                        _that._latestPosition,
                        Math.Min(textSourcePosition + line.Length - 1, _that._text.Length - 1)
                        );

                    GenericTextRunProperties lastRunProps = thatFormatRider.CurrentElement as GenericTextRunProperties;

                    TextCollapsingProperties trailingEllipsis;

                    if (_that._trimming == TextTrimming.CharacterEllipsis)
                        trailingEllipsis = new TextTrailingCharacterEllipsis(maxLineLength, lastRunProps);
                    else
                    {
                        Debug.Assert(_that._trimming == TextTrimming.WordEllipsis);
                        trailingEllipsis = new TextTrailingWordEllipsis(maxLineLength, lastRunProps);
                    }

                    TextLine collapsedLine = line.Collapse(trailingEllipsis);

                    if (collapsedLine != line)
                    {
                        line.Dispose();
                        line = collapsedLine;
                    }
                }
                return line;
            }


            /// <summary>
            /// Sets the enumerator to its initial position,
            /// which is before the first element in the collection
            /// </summary>
            public void Reset()
            {
                _textStorePosition = 0;
                _lineCount = 0;
                _totalHeight = 0;
                _currentLine = null;
                _nextLine = null;
            }
        }

        /// <summary>
        /// Returns an enumerator that can iterate through the text line collection
        /// </summary>
        private LineEnumerator GetEnumerator()
        {
            return new LineEnumerator(this);
        }
#if NEVER
        /// <summary>
        /// Returns an enumerator that can iterate through the text line collection
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
#endif

        private void AdvanceLineOrigin(ref Point lineOrigin, TextLine currentLine)
        {
            double height = currentLine.Height;
            // advance line origin according to the flow direction
            switch (_defaultParaProps.FlowDirection)
            {
                case FlowDirection.LeftToRight:
                case FlowDirection.RightToLeft:
                    lineOrigin.Y += height;
                    break;
            }
        }

        #endregion

        #region Measurement and layout properties

        private class CachedMetrics
        {
            // vertical
            public double Height;
            public double Baseline;

            // horizontal
            public double Width;
            public double WidthIncludingTrailingWhitespace;

            // vertical bounding box metrics
            public double Extent;
            public double OverhangAfter;

            // horizontal bounding box metrics
            public double OverhangLeading;
            public double OverhangTrailing;
        }

        /// <summary>
        /// Defines the flow direction
        /// </summary>
        public FlowDirection FlowDirection
        {
            set
            {
                ValidateFlowDirection(value, "value");
                _defaultParaProps.SetFlowDirection(value);
                InvalidateMetrics();
            }
            get
            {
                return _defaultParaProps.FlowDirection;
            }
        }

        /// <summary>
        /// Defines the alignment of text within the column
        /// </summary>
        public TextAlignment TextAlignment
        {
            set
            {
                _defaultParaProps.SetTextAlignment(value);
                InvalidateMetrics();
            }
            get
            {
                return _defaultParaProps.TextAlignment;
            }
        }

        /// <summary>
        /// Gets or sets the height of, or the spacing between, each line where
        /// zero represents the default line height.
        /// </summary>
        public double LineHeight
        {
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value", SR.Get(SRID.ParameterCannotBeNegative));

                _defaultParaProps.SetLineHeight(value);
                InvalidateMetrics();
            }
            get
            {
                return _defaultParaProps.LineHeight;
            }
        }

        /// <summary>
        /// The MaxTextWidth property defines the alignment edges for the FormattedText.
        /// For example, left aligned text is wrapped such that the leftmost glyph alignment point
        /// on each line falls exactly on the left edge of the rectangle.
        /// Note that for many fonts, especially in italic style, some glyph strokes may extend beyond the edges of the alignment rectangle.
        /// For this reason, it is recommended that clients draw text with at least 1/6 em (i.e of the font size) unused margin space either side.
        /// Zero value of MaxTextWidth is equivalent to the maximum possible paragraph width.
        /// </summary>
        public double MaxTextWidth
        {
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value", SR.Get(SRID.ParameterCannotBeNegative));
                _maxTextWidth = value;
                InvalidateMetrics();
            }
            get
            {
                return _maxTextWidth;
            }
        }

        /// <summary>
        /// Sets the array of lengths,
        /// which will be applied to each line of text in turn.
        /// If the text covers more lines than there are entries in the length array,
        /// the last entry is reused as many times as required.
        /// The maxTextWidths array overrides the MaxTextWidth property.
        /// </summary>
        /// <param name="maxTextWidths">The max text width array</param>
        public void SetMaxTextWidths(double [] maxTextWidths)
        {
            if (maxTextWidths == null || maxTextWidths.Length <= 0)
                throw new ArgumentNullException("maxTextWidths");
            _maxTextWidths = maxTextWidths;
            InvalidateMetrics();
        }

        /// <summary>
        /// Obtains a copy of the array of lengths,
        /// which will be applied to each line of text in turn.
        /// If the text covers more lines than there are entries in the length array,
        /// the last entry is reused as many times as required.
        /// The maxTextWidths array overrides the MaxTextWidth property.
        /// </summary>
        /// <returns>The copy of max text width array</returns>
        public double [] GetMaxTextWidths()
        {
            return (_maxTextWidths == null) ? null : (double [])_maxTextWidths.Clone();
        }

        /// <summary>
        /// Sets the maximum length of a column of text.
        /// The last line of text displayed is the last whole line that will fit within this limit,
        /// or the nth line as specified by MaxLineCount, whichever occurs first.
        /// Use the Trimming property to control how the omission of text is indicated.
        /// </summary>
        public double MaxTextHeight
        {
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException("value", SR.Get(SRID.PropertyMustBeGreaterThanZero, "MaxTextHeight"));

                if (DoubleUtil.IsNaN(value))
                    throw new ArgumentOutOfRangeException("value", SR.Get(SRID.PropertyValueCannotBeNaN, "MaxTextHeight"));

                _maxTextHeight = value;
                InvalidateMetrics();
            }
            get
            {
                return _maxTextHeight;
            }
        }

        /// <summary>
        /// Defines the maximum number of lines to display.
        /// The last line of text displayed is the lineCount-1'th line,
        /// or the last whole line that will fit within the count set by MaxTextHeight,
        /// whichever occurs first.
        /// Use the Trimming property to control how the omission of text is indicated
        /// </summary>
        public int MaxLineCount
        {
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException("value", SR.Get(SRID.ParameterMustBeGreaterThanZero));
                _maxLineCount = value;
                InvalidateMetrics();
            }
            get
            {
                return _maxLineCount;
            }
        }


        /// <summary>
        /// Defines how omission of text is indicated.
        /// CharacterEllipsis trimming allows partial words to be displayed,
        /// while WordEllipsis removes whole words to fit.
        /// Both guarantee to include an ellipsis ('...') at the end of the lines
        /// where text has been trimmed as a result of line and column limits.
        /// </summary>
        public TextTrimming Trimming
        {
            set
            {
                if ((int)value < 0 || (int)value > (int)TextTrimming.WordEllipsis)
                    throw new InvalidEnumArgumentException("value", (int)value, typeof(TextTrimming));

                _trimming = value;
                if (_trimming == TextTrimming.None)
                {
                    // if trimming is disabled, enforce emergency wrap
                    _defaultParaProps.SetTextWrapping(TextWrapping.Wrap);
                }
                else 
                {
                    _defaultParaProps.SetTextWrapping(TextWrapping.WrapWithOverflow);
                }

                InvalidateMetrics();
            }
            get
            {
                return _trimming;
            }
        }


        /// <summary>
        /// Lazily initializes the cached metrics EXCEPT for black box metrics and
        /// returns the CachedMetrics structure.
        /// </summary>
        private CachedMetrics Metrics
        {
            get
            {
                if (_metrics == null)
                {
                    // We need to obtain the metrics. DON'T compute black box metrics here because
                    // they probably won't be needed and computing them requires GlyphRun creation. 
                    // In the common case where a client measures and then draws, we'll format twice 
                    // but create GlyphRuns only during drawing.

                    _metrics = DrawAndCalculateMetrics(
                        null,           // drawing context
                        new Point(),    // drawing offset
                        false);         // don't calculate black box metrics
                }
                return _metrics;
            }
        }


        /// <summary>
        /// Lazily initializes the cached metrics INCLUDING black box metrics and
        /// returns the CachedMetrics structure.
        /// </summary>
        private CachedMetrics BlackBoxMetrics
        {
            get
            {
                if (_metrics == null || double.IsNaN(_metrics.Extent))
                {
                    // We need to obtain the metrics, including black box metrics.

                    _metrics = DrawAndCalculateMetrics(
                        null,           // drawing context
                        new Point(),    // drawing offset
                        true);          // calculate black box metrics
                }
                return _metrics;
            }
        }


        /// <summary>
        /// The distance from the top of the first line to the bottom of the last line.
        /// </summary>
        public double Height
        {
            get
            {
                return Metrics.Height;
            }
        }

        /// <summary>
        /// The distance from the topmost black pixel of the first line
        /// to the bottommost black pixel of the last line. 
        /// </summary>
        public double Extent
        {
            get
            {
                return BlackBoxMetrics.Extent;
            }
        }

        /// <summary>
        /// The distance from the top of the first line to the baseline of the first line.
        /// </summary>
        public double Baseline
        {
            get
            {
                return Metrics.Baseline;
            }
        }

        /// <summary>
        /// The distance from the bottom of the last line to the extent bottom.
        /// </summary>
        public double OverhangAfter
        {
            get
            {
                return BlackBoxMetrics.OverhangAfter;
            }
        }

        /// <summary>
        /// The maximum distance from the leading black pixel to the leading alignment point of a line.
        /// </summary>
        public double OverhangLeading
        {
            get
            {
                return BlackBoxMetrics.OverhangLeading;
            }
        }

        /// <summary>
        /// The maximum distance from the trailing black pixel to the trailing alignment point of a line.
        /// </summary>
        public double OverhangTrailing
        {
            get
            {
                return BlackBoxMetrics.OverhangTrailing;
            }
        }

        /// <summary>
        /// The maximum advance width between the leading and trailing alignment points of a line,
        /// excluding the width of whitespace characters at the end of the line.
        /// </summary>
        public double Width
        {
            get
            {
                return Metrics.Width;
            }
        }

        /// <summary>
        /// The maximum advance width between the leading and trailing alignment points of a line,
        /// including the width of whitespace characters at the end of the line.
        /// </summary>
        public double WidthIncludingTrailingWhitespace
        {
            get
            {
                return Metrics.WidthIncludingTrailingWhitespace;
            }
        }

        /// <summary>
        /// The minimum line width that can be specified without causing any word to break. 
        /// </summary>
        public double MinWidth
        {
            get
            {
                if (_minWidth != double.MinValue)
                    return _minWidth;

                if (_textSourceImpl == null)
                    _textSourceImpl = new TextSourceImplementation(this);

                _minWidth = TextFormatter.FromCurrentDispatcher(_textFormattingMode).FormatMinMaxParagraphWidth(
                    _textSourceImpl,
                    0,  // textSourceCharacterIndex
                    _defaultParaProps
                    ).MinWidth;

                return _minWidth;
            }
        }


        /// <summary>
        /// Builds a highlight geometry object.
        /// </summary>
        /// <param name="origin">The origin of the highlight region</param>
        /// <returns>Geometry that surrounds the text.</returns>
        public Geometry BuildHighlightGeometry(Point origin)
        {
            return BuildHighlightGeometry(origin, 0, _text.Length);
        }

        /// <summary>
        /// Obtains geometry for the text, including underlines and strikethroughs. 
        /// </summary>
        /// <param name="origin">The left top origin of the resulting geometry.</param>
        /// <returns>The geometry returned contains the combined geometry
        /// of all of the glyphs, underlines and strikeThroughs that represent the formatted text.
        /// Overlapping contours are merged by performing a Boolean union operation.</returns>
        public Geometry BuildGeometry(Point origin)
        {
            GeometryGroup accumulatedGeometry = null;
            Point lineOrigin = origin;

            DrawingGroup drawing = new DrawingGroup();
            DrawingContext ctx = drawing.Open();

            // we can't use foreach because it requires GetEnumerator and associated classes to be public
            // foreach (TextLine currentLine in this)

            using (LineEnumerator enumerator = GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    using (TextLine currentLine = enumerator.Current)
                    {
                        currentLine.Draw(ctx, lineOrigin, InvertAxes.None);
                        AdvanceLineOrigin(ref lineOrigin, currentLine);
                    }
                }
            }

            ctx.Close();

            //  recursively go down the DrawingGroup to build up the geometry
            CombineGeometryRecursive(drawing, ref accumulatedGeometry);

            // Make sure to always return Geometry.Empty from public methods for empty geometries.
            if (accumulatedGeometry == null || accumulatedGeometry.IsEmpty())
                return Geometry.Empty;
            return accumulatedGeometry;
        }        

        /// <summary>
        /// Builds a highlight geometry object for a given character range.
        /// </summary>
        /// <param name="origin">The origin of the highlight region.</param>
        /// <param name="startIndex">The start index of initial character the bounds should be obtained for.</param>
        /// <param name="count">The number of characters the bounds should be obtained for.</param>
        /// <returns>Geometry that surrounds the specified character range.</returns>
        public Geometry BuildHighlightGeometry(Point origin, int startIndex, int count)
        {
            ValidateRange(startIndex, count);

            PathGeometry accumulatedBounds = null;
            using (LineEnumerator enumerator = GetEnumerator())
            {
                Point lineOrigin = origin;

                while (enumerator.MoveNext())
                {
                    using (TextLine currentLine = enumerator.Current)
                    {
                        int x0 = Math.Max(enumerator.Position, startIndex);
                        int x1 = Math.Min(enumerator.Position + enumerator.Length, startIndex + count);

                        // check if this line is intersects with the specified character range
                        if (x0 < x1)
                        {
                            IList<TextBounds> highlightBounds = currentLine.GetTextBounds(
                                x0,
                                x1 - x0
                                );

                            if (highlightBounds != null)
                            {
                                foreach (TextBounds bound in highlightBounds)
                                {
                                    Rect rect = bound.Rectangle;

                                    if (FlowDirection == FlowDirection.RightToLeft)
                                    {
                                        // Convert logical units (which extend leftward from the right edge
                                        // of the paragraph) to physical units.
                                        //
                                        // Note that since rect is in logical units, rect.Right corresponds to 
                                        // the visual *left* edge of the rectangle in the RTL case. Specifically,
                                        // is the distance leftward from the right edge of the formatting rectangle
                                        // whose width is the paragraph width passed to FormatLine.
                                        //
                                        rect.X = enumerator.CurrentParagraphWidth - rect.Right;
                                    }

                                    rect.X += lineOrigin.X;
                                    rect.Y += lineOrigin.Y;

                                    RectangleGeometry rectangleGeometry = new RectangleGeometry(rect);
                                    if (accumulatedBounds == null)
                                        accumulatedBounds = rectangleGeometry.GetAsPathGeometry();
                                    else
                                        accumulatedBounds = Geometry.Combine(accumulatedBounds, rectangleGeometry, GeometryCombineMode.Union, null);
                                }
                            }
                        }
                        AdvanceLineOrigin(ref lineOrigin, currentLine);
                    }
                }
            }

            if (accumulatedBounds == null  ||  accumulatedBounds.IsEmpty())
                return null;

            return accumulatedBounds;
        }

        #endregion

        #region Drawing
        /// <summary>
        /// Draws the text object
        /// </summary>
        internal void Draw(
            DrawingContext  dc, 
            Point           origin
            )
        {
            Point lineOrigin = origin;

            if (_metrics != null && !double.IsNaN(_metrics.Extent))
            {
                // we can't use foreach because it requires GetEnumerator and associated classes to be public
                // foreach (TextLine currentLine in this)

                using (LineEnumerator enumerator = GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        using (TextLine currentLine = enumerator.Current)
                        {
                            currentLine.Draw(dc, lineOrigin, InvertAxes.None);
                            AdvanceLineOrigin(ref lineOrigin, currentLine);
                        }
                    }
                }
            }
            else
            {
                // Calculate metrics as we draw to avoid formatting again if we need metrics later; we compute
                // black box metrics too because these are already known as a side-effect of drawing

                _metrics = DrawAndCalculateMetrics(dc, origin, true);
            }
        }

        private CachedMetrics DrawAndCalculateMetrics(DrawingContext dc, Point drawingOffset, bool getBlackBoxMetrics)
        {
            // The calculation for FormattedText.Width and Overhangs was wrong for Right and Center alignment.
            // Thus the fix of this bug is based on the fact that FormattedText always had 0 indent and no 
            // TextMarkerProperties. These assumptions enabled us to remove TextLine.Start from the calculation 
            // of the Width. TextLine.Start caused the calculation of FormattedText to be incorrect in cases 
            // of Right and Center alignment because it took on -ve values when ParagraphWidth was 0 (which indicates infinite width). 
            // This was a result of how TextFormatter interprets TextLine.Start. In the simplest case, it computes 
            // TextLine.Start as Paragraph Width - Line Width (for Right alignment).
            // So, the following two Debug.Asserts verify that the assumptions over which the bug fix was made are still valid 
            // and not changed by adding features to FormattedText. Incase these assumptions were invalidated, the bug fix 
            // should be revised and it will possibly involve alot of changes elsewhere.
            Debug.Assert(_defaultParaProps.Indent == 0.0, "FormattedText was assumed to always have 0 indent. This assumption has changed and thus the calculation of Width and Overhangs should be revised.");
            Debug.Assert(_defaultParaProps.TextMarkerProperties == null, "FormattedText was assumed to always have no TextMarkerProperties. This assumption has changed and thus the calculation of Width and Overhangs should be revised.");
            CachedMetrics metrics = new CachedMetrics();

            if (_text.Length == 0)
            {
                return metrics;
            }

            // we can't use foreach because it requires GetEnumerator and associated classes to be public
            // foreach (TextLine currentLine in this)

            using (LineEnumerator enumerator = GetEnumerator())
            {
                bool first = true;

                double accBlackBoxLeft, accBlackBoxTop, accBlackBoxRight, accBlackBoxBottom;
                accBlackBoxLeft = accBlackBoxTop = double.MaxValue;
                accBlackBoxRight = accBlackBoxBottom = double.MinValue;

                Point origin = new Point(0, 0);

                // Holds the TextLine.Start of the longest line. Thus it will hold the minimum value 
                // of TextLine.Start among all the lines that forms the text. The overhangs (leading and trailing) 
                // are calculated with an offset as a result of the same issue with TextLine.Start. 
                // So, we compute this offset and remove it later from the values of the overhangs.
                double lineStartOfLongestLine = Double.MaxValue;
                while (enumerator.MoveNext())
                {
                    // enumerator will dispose the currentLine
                    using (TextLine currentLine = enumerator.Current)
                    {
                        // if we're drawing, do it first as this will compute black box metrics as a side-effect
                        if (dc != null)
                        {
                            currentLine.Draw(
                                dc,
                                new Point(origin.X + drawingOffset.X, origin.Y + drawingOffset.Y),
                                InvertAxes.None
                                );
                        }

                        if (getBlackBoxMetrics)
                        {
                            double blackBoxLeft = origin.X + currentLine.Start + currentLine.OverhangLeading;
                            double blackBoxRight = origin.X + currentLine.Start + currentLine.Width - currentLine.OverhangTrailing;
                            double blackBoxBottom = origin.Y + currentLine.Height + currentLine.OverhangAfter;
                            double blackBoxTop = blackBoxBottom - currentLine.Extent;

                            accBlackBoxLeft = Math.Min(accBlackBoxLeft, blackBoxLeft);
                            accBlackBoxRight = Math.Max(accBlackBoxRight, blackBoxRight);
                            accBlackBoxBottom = Math.Max(accBlackBoxBottom, blackBoxBottom);
                            accBlackBoxTop = Math.Min(accBlackBoxTop, blackBoxTop);

                            metrics.OverhangAfter = currentLine.OverhangAfter;
                        }

                        metrics.Height += currentLine.Height;
                        metrics.Width = Math.Max(metrics.Width, currentLine.Width);
                        metrics.WidthIncludingTrailingWhitespace = Math.Max(metrics.WidthIncludingTrailingWhitespace, currentLine.WidthIncludingTrailingWhitespace);
                        lineStartOfLongestLine = Math.Min(lineStartOfLongestLine, currentLine.Start);

                        if (first)
                        {
                            metrics.Baseline = currentLine.Baseline;
                            first = false;
                        }

                        AdvanceLineOrigin(ref origin, currentLine);
                    }
                }

                if (getBlackBoxMetrics)
                {
                    metrics.Extent = accBlackBoxBottom - accBlackBoxTop;
                    metrics.OverhangLeading = accBlackBoxLeft - lineStartOfLongestLine;
                    metrics.OverhangTrailing = metrics.Width - (accBlackBoxRight - lineStartOfLongestLine);
                }
                else
                {
                    // indicate that black box metrics are not known
                    metrics.Extent = double.NaN;
                }
            }

            return metrics;
        }

        #endregion

        #region TextSource implementation

        private class TextSourceImplementation : TextSource
        {
            private FormattedText   _that;

            public TextSourceImplementation(FormattedText text)
            {
                _that = text;
                PixelsPerDip = _that.PixelsPerDip;
            }

            /// <summary>
            /// TextFormatter to get a text run started at specified text source position
            /// </summary>
            /// <param name="textSourceCharacterIndex">character index to specify where in the source text the fetch is to start.</param>
            public override TextRun GetTextRun(
                int         textSourceCharacterIndex
                )
            {
                if (textSourceCharacterIndex >= _that._text.Length)
                {
                    return new TextEndOfParagraph(1);
                }

                SpanRider thatFormatRider = new SpanRider(
                    _that._formatRuns, 
                    _that._latestPosition,
                    textSourceCharacterIndex
                    );

                TextRunProperties properties = thatFormatRider.CurrentElement as GenericTextRunProperties;
                TextCharacters textCharacters = new TextCharacters(_that._text,
                    textSourceCharacterIndex,
                    thatFormatRider.Length,
                    properties
                    );
                properties.PixelsPerDip = this.PixelsPerDip;
                return textCharacters;
            }


            /// <summary>
            /// TextFormatter to get text immediately before specified text source position.
            /// </summary>
            /// <param name="textSourceCharacterIndexLimit">character index to specify where in the source text the text retrieval stops.</param>
            /// <returns>character string immediately before the specify text source character index.</returns>
            public override TextSpan<CultureSpecificCharacterBufferRange> GetPrecedingText(
                int         textSourceCharacterIndexLimit
                )
            {
                CharacterBufferRange charString = CharacterBufferRange.Empty;
                CultureInfo culture = null;
            
                if (textSourceCharacterIndexLimit > 0)                    
                {
                    SpanRider thatFormatRider = new SpanRider(
                        _that._formatRuns,
                        _that._latestPosition,
                        textSourceCharacterIndexLimit - 1
                        );
                    
                    charString = new CharacterBufferRange(
                        new CharacterBufferReference(_that._text, thatFormatRider.CurrentSpanStart),
                        textSourceCharacterIndexLimit - thatFormatRider.CurrentSpanStart
                        );

                    culture = ((TextRunProperties)thatFormatRider.CurrentElement).CultureInfo;
                }

                return new TextSpan<CultureSpecificCharacterBufferRange> (
                    charString.Length,
                    new CultureSpecificCharacterBufferRange(culture, charString)
                    );
            }

            /// <summary>
            /// TextFormatter to map a text source character index to a text effect character index        
            /// </summary>
            /// <param name="textSourceCharacterIndex"> text source character index </param>
            /// <returns> the text effect index corresponding to the text effect character index </returns>
            public override int GetTextEffectCharacterIndexFromTextSourceCharacterIndex(
                int textSourceCharacterIndex
                )
            {                
                throw new NotSupportedException();
            }
        };

        #endregion

        #region private methods
        private void CombineGeometryRecursive(Drawing drawing, ref GeometryGroup accumulatedGeometry)
        {
            DrawingGroup group = drawing as DrawingGroup;
            if (group != null)
            {
                // recursively go down for DrawingGroup
                foreach (Drawing child in group.Children)
                {
                    CombineGeometryRecursive(child, ref accumulatedGeometry);
                }
            }
            else 
            {
                GlyphRunDrawing glyphRunDrawing = drawing as GlyphRunDrawing;
                if (glyphRunDrawing != null)
                {
                    // process glyph run
                    GlyphRun glyphRun = glyphRunDrawing.GlyphRun;
                    if (glyphRun != null)
                    {
                        Geometry glyphRunGeometry = glyphRun.BuildGeometry();
                        
                        if (!glyphRunGeometry.IsEmpty())
                        {
                            if (accumulatedGeometry == null)
                            {
                                accumulatedGeometry = new GeometryGroup();
                                accumulatedGeometry.FillRule = FillRule.Nonzero;
                            }
                            accumulatedGeometry.Children.Add(glyphRunGeometry);                        
                        }
                    }
                }
                else
                {
                    GeometryDrawing geometryDrawing = drawing as GeometryDrawing;
                    if (geometryDrawing != null)
                    {
                        // process geometry (i.e. TextDecoration on the line)
                        Geometry geometry = geometryDrawing.Geometry;
                         
                        if (geometry != null)
                        {              
                            LineGeometry lineGeometry = geometry as LineGeometry;
                            if (lineGeometry != null)
                            {
                                // For TextDecoration drawn by DrawLine(), the geometry is a LineGeometry which has no 
                                // bounding area. So this line won't show up. Work aroud it by increase the Bounding rect 
                                // to be Pen's thickness                        

                                Rect bound  = lineGeometry.Bounds;
                                if (bound.Height == 0)
                                {
                                    bound.Height = geometryDrawing.Pen.Thickness;
                                }                        
                                else if (bound.Width == 0)
                                {
                                    bound.Width = geometryDrawing.Pen.Thickness;
                                } 

                                // convert the line geometry into a rectangle geometry
                                // we lost line cap info here
                                geometry = new RectangleGeometry(bound);
                            }
                            if (accumulatedGeometry == null)
                            {
                                accumulatedGeometry = new GeometryGroup();
                                accumulatedGeometry.FillRule = FillRule.Nonzero;
                            }
                            accumulatedGeometry.Children.Add(geometry);
                        }
                    }
                }            
            }
        }
        #endregion

        #region Private fields

        // properties and format runs
        private string                          _text;
        private double                          _pixelsPerDip = MS.Internal.FontCache.Util.PixelsPerDip;
        private SpanVector                      _formatRuns = new SpanVector(null);
        private SpanPosition                    _latestPosition = new SpanPosition();

        private GenericTextParagraphProperties  _defaultParaProps;

        private double                          _maxTextWidth;
        private double []                       _maxTextWidths;
        private double                          _maxTextHeight = double.MaxValue;
        private int                             _maxLineCount = int.MaxValue;
        private TextTrimming                    _trimming = TextTrimming.WordEllipsis;

        private TextFormattingMode              _textFormattingMode;

        // text source callbacks
        private TextSourceImplementation        _textSourceImpl;

        // cached metrics
        private CachedMetrics                   _metrics;
        private double                          _minWidth;

        #endregion

        #region Constants

        const double MaxFontEmSize = Constants.RealInfiniteWidth / Constants.GreatestMutiplierOfEm;

        #endregion
    }
}

