// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Glyphs element for fixed text rendering.
//
// Spec: Glyphs element and GlyphRun object.htm
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows.Threading;


using System.Windows;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Markup;
using System.ComponentModel;
using System.Security;

using MS.Utility;
using MS.Internal.Navigation;
using MS.Internal.Utility;
using MS.Internal;

using BuildInfo=MS.Internal.PresentationFramework.BuildInfo;

namespace System.Windows.Documents
{
    /// <summary>
    /// Glyphs shape represents GlyphRun in markup
    /// </summary>
    public sealed class Glyphs : FrameworkElement, IUriContext
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        public Glyphs()
        {
        }

        #endregion Constructors


        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods


        /// <summary>
        /// Creates a GlyphRun object from the properties on a Glyphs object.
        /// </summary>
        /// <returns>GlyphRun object that corresponds to the properties set on this Glyphs object.</returns>
        public GlyphRun ToGlyphRun()
        {
            ComputeMeasurementGlyphRunAndOrigin();
            if (_measurementGlyphRun == null)
                return null;
            Debug.Assert(_glyphRunProperties != null);

            return _measurementGlyphRun;
        }

        #endregion Public Methods


        #region IUriContext implementation

        /// <summary>
        /// IUriContext interface is implemented by Glyphs element so that it
        /// can hold on to the base URI used by parser.
        /// The base URI is needed to resolve FontUri property.
        /// </summary>
        /// <value>Base Uri</value>
        Uri IUriContext.BaseUri
        {
            get
            {
                return (Uri)GetValue(BaseUriHelper.BaseUriProperty);
            }
            set
            {
                SetValue(BaseUriHelper.BaseUriProperty, value);
            }
        }
        #endregion IUriContext implementation

        #region Layout and rendering

        /// <summary>
        /// ArrangeOverride sets the "shapeBounds" in for the shape.
        /// </summary>
        protected override Size ArrangeOverride(Size finalSize)
        {
            base.ArrangeOverride(finalSize);

            Rect inkBoundingBox;

            if (_measurementGlyphRun != null)
                inkBoundingBox = _measurementGlyphRun.ComputeInkBoundingBox();
            else
                inkBoundingBox = Rect.Empty;

            if (!inkBoundingBox.IsEmpty)
            {
                inkBoundingBox.X += _glyphRunOrigin.X;
                inkBoundingBox.Y += _glyphRunOrigin.Y;
            }
            return finalSize;
        }

        /// <summary>
        /// Renders GlyphRun into a drawing context
        /// </summary>
        /// <param name="context">Drawing context</param>
        protected override void OnRender(DrawingContext context)
        {
            if (_glyphRunProperties == null || _measurementGlyphRun == null)
                return;

            context.PushGuidelineY1(_glyphRunOrigin.Y);
            try 
            {
                context.DrawGlyphRun(Fill, _measurementGlyphRun);
            }
            finally 
            {
                context.Pop();
            }
        }

        /// <summary>
        /// Measurement override for Glyphs
        /// </summary>
        /// <param name="constraint">Input constraint</param>
        /// <returns></returns>
        protected override Size MeasureOverride(Size constraint)
        {
            ComputeMeasurementGlyphRunAndOrigin();

            if (_measurementGlyphRun == null)
                return new Size();

            Rect designRect = _measurementGlyphRun.ComputeAlignmentBox();

            designRect.Offset(_glyphRunOrigin.X, _glyphRunOrigin.Y);

            return new Size(
                Math.Max(0, designRect.Right),
                Math.Max(0, designRect.Bottom)
            );
        }

        #endregion Layout and rendering

        #region Parsing and GlyphRun creation

        private void ComputeMeasurementGlyphRunAndOrigin()
        {
            if (_glyphRunProperties == null)
            {
                _measurementGlyphRun = null;
                ParseGlyphRunProperties();

                if (_glyphRunProperties == null)
                {
                    return;
                }
            }
            else if (_measurementGlyphRun != null)
            {
                return;
            }

            bool leftToRight = ((BidiLevel & 1) == 0);

            bool haveOriginX = !DoubleUtil.IsNaN(OriginX);
            bool haveOriginY = !DoubleUtil.IsNaN(OriginY);

            bool measurementGlyphRunOriginValid = false;

            Rect alignmentRect = new Rect();
            if (haveOriginX && haveOriginY && leftToRight)
            {
                _measurementGlyphRun = _glyphRunProperties.CreateGlyphRun(new Point(OriginX,OriginY), Language);
                measurementGlyphRunOriginValid = true;
            }
            else
            {
                _measurementGlyphRun = _glyphRunProperties.CreateGlyphRun(new Point(), Language);
                // compute alignment box for origins
                alignmentRect = _measurementGlyphRun.ComputeAlignmentBox();
            }

            if (haveOriginX)
                _glyphRunOrigin.X = OriginX;
            else
                _glyphRunOrigin.X = leftToRight ? 0 : alignmentRect.Width;

            if (haveOriginY)
                _glyphRunOrigin.Y = OriginY;
            else
                _glyphRunOrigin.Y = -alignmentRect.Y;

            if (!measurementGlyphRunOriginValid)
            {
                _measurementGlyphRun = _glyphRunProperties.CreateGlyphRun(_glyphRunOrigin, Language);
            }
        }

        private void ParseCaretStops(LayoutDependentGlyphRunProperties glyphRunProperties)
        {
            string caretStopsString = CaretStops;
            if (String.IsNullOrEmpty(caretStopsString))
            {
                glyphRunProperties.caretStops = null;
                return;
            }

            // Caret stop count should be equal to the number of UTF16 code points in the glyph run plus one.
            // Logic below is similar to GlyphRun.CodepointCount property.

            int caretStopCount;

            if (!String.IsNullOrEmpty(glyphRunProperties.unicodeString))
                caretStopCount = glyphRunProperties.unicodeString.Length + 1;
            else
            {
                if (glyphRunProperties.clusterMap != null && glyphRunProperties.clusterMap.Length != 0)
                    caretStopCount = glyphRunProperties.clusterMap.Length + 1;
                else
                {
                    Debug.Assert(glyphRunProperties.glyphIndices != null);
                    caretStopCount = glyphRunProperties.glyphIndices.Length + 1;
                }
            }

            bool[] caretStops = new bool[caretStopCount];

            int i = 0;
            foreach (char c in caretStopsString)
            {
                if (Char.IsWhiteSpace(c))
                    continue;

                int nibble;

                if ('0' <= c && c <= '9')
                    nibble = c - '0';
                else if ('a' <= c && c <= 'f')
                    nibble = c - 'a' + 10;
                else if ('A' <= c && c <= 'F')
                    nibble = c - 'A' + 10;
                else
                    throw new ArgumentException(SR.Get(SRID.GlyphsCaretStopsContainsHexDigits), "CaretStops");

                Debug.Assert(0 <= nibble && nibble <= 15);

                if ((nibble & 8) != 0)
                {
                    if (i >= caretStops.Length)
                        throw new ArgumentException(SR.Get(SRID.GlyphsCaretStopsLengthCorrespondsToUnicodeString), "CaretStops");
                    caretStops[i] = true;
                }
                ++i;
                if ((nibble & 4) != 0)
                {
                    if (i >= caretStops.Length)
                        throw new ArgumentException(SR.Get(SRID.GlyphsCaretStopsLengthCorrespondsToUnicodeString), "CaretStops");
                    caretStops[i] = true;
                }
                ++i;
                if ((nibble & 2) != 0)
                {
                    if (i >= caretStops.Length)
                        throw new ArgumentException(SR.Get(SRID.GlyphsCaretStopsLengthCorrespondsToUnicodeString), "CaretStops");
                    caretStops[i] = true;
                }
                ++i;
                if ((nibble & 1) != 0)
                {
                    if (i >= caretStops.Length)
                        throw new ArgumentException(SR.Get(SRID.GlyphsCaretStopsLengthCorrespondsToUnicodeString), "CaretStops");
                    caretStops[i] = true;
                }
                ++i;
            }

            // If the number of entries in the caret stop specification string is less than the number of code points,
            // set the remaining caret stop values to true.
            while (i < caretStops.Length)
            {
                caretStops[i++] = true;
            }
            glyphRunProperties.caretStops = caretStops;
        }

        private void ParseGlyphRunProperties()
        {
            LayoutDependentGlyphRunProperties glyphRunProperties = null;
            Uri uri = FontUri;

            if (uri != null)
            {
                // Indices and UnicodeString cannot both be empty.
                if (String.IsNullOrEmpty(UnicodeString) && String.IsNullOrEmpty(Indices))
                    throw new ArgumentException(SR.Get(SRID.GlyphsUnicodeStringAndIndicesCannotBothBeEmpty));

                glyphRunProperties = new LayoutDependentGlyphRunProperties(GetDpi().PixelsPerDip);

                if (!uri.IsAbsoluteUri)
                {
                    uri = BindUriHelper.GetResolvedUri(BaseUriHelper.GetBaseUri(this), uri);
                }

                glyphRunProperties.glyphTypeface = new GlyphTypeface(uri, StyleSimulations);

                glyphRunProperties.unicodeString = UnicodeString;
                glyphRunProperties.sideways = IsSideways;
                glyphRunProperties.deviceFontName = DeviceFontName;

                // parse the Indices property
                List<ParsedGlyphData> parsedGlyphs;
                int glyphCount = ParseGlyphsProperty(
                    glyphRunProperties.glyphTypeface,
                    glyphRunProperties.unicodeString,
                    glyphRunProperties.sideways,
                    out parsedGlyphs,
                    out glyphRunProperties.clusterMap);

                Debug.Assert(parsedGlyphs.Count == glyphCount);

                glyphRunProperties.glyphIndices = new ushort[glyphCount];
                glyphRunProperties.advanceWidths = new double[glyphCount];

                ParseCaretStops(glyphRunProperties);

                // Delay creating glyphOffsets array because in many common cases it will contain only zeroed entries.
                glyphRunProperties.glyphOffsets = null;

                int i = 0;

                glyphRunProperties.fontRenderingSize = FontRenderingEmSize;
                glyphRunProperties.bidiLevel = BidiLevel;

                double fromEmToMil = glyphRunProperties.fontRenderingSize / EmMultiplier;

                foreach (ParsedGlyphData parsedGlyphData in parsedGlyphs)
                {
                    glyphRunProperties.glyphIndices[i] = parsedGlyphData.glyphIndex;

                    // convert advances and offsets from integers in em space to doubles coordinates in MIL space
                    glyphRunProperties.advanceWidths[i] = parsedGlyphData.advanceWidth * fromEmToMil;

                    if (parsedGlyphData.offsetX != 0 || parsedGlyphData.offsetY != 0)
                    {
                        // Lazily create glyph offset array. Previous entries will be correctly set to zero
                        // by the default Point ctor.
                        if (glyphRunProperties.glyphOffsets == null)
                            glyphRunProperties.glyphOffsets = new Point[glyphCount];

                        glyphRunProperties.glyphOffsets[i].X = parsedGlyphData.offsetX * fromEmToMil;
                        glyphRunProperties.glyphOffsets[i].Y = parsedGlyphData.offsetY * fromEmToMil;
                    }

                    ++i;
                }
            }
            _glyphRunProperties = glyphRunProperties;
        }

        private static bool IsEmpty(string s)
        {
            foreach (char c in s)
            {
                if (!Char.IsWhiteSpace(c))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Read GlyphIndex specification - glyph index value with an optional glyph cluster prefix.
        /// </summary>
        /// <param name="valueSpec"></param>
        /// <param name="inCluster"></param>
        /// <param name="glyphClusterSize"></param>
        /// <param name="characterClusterSize"></param>
        /// <param name="glyphIndex"></param>
        /// <returns>true if glyph index is present, false if glyph index is not present.</returns>
        private bool ReadGlyphIndex(
            string      valueSpec,
            ref bool    inCluster,
            ref int     glyphClusterSize,
            ref int     characterClusterSize,
            ref ushort  glyphIndex)
        {
            // the format is ... [(CharacterClusterSize[:GlyphClusterSize])] GlyphIndex ...
            string glyphIndexString = valueSpec;

            int firstBracket = valueSpec.IndexOf('(');
            if (firstBracket != -1)
            {
                // Only spaces are allowed before the bracket
                for (int i=0; i<firstBracket; i++)
                {
                    if (!Char.IsWhiteSpace(valueSpec[i]))
                        throw new ArgumentException(SR.Get(SRID.GlyphsClusterBadCharactersBeforeBracket));
                }

                if (inCluster)
                    throw new ArgumentException(SR.Get(SRID.GlyphsClusterNoNestedClusters));

                int secondBracket = valueSpec.IndexOf(')');
                if (secondBracket == -1 || secondBracket <= firstBracket + 1)
                    throw new ArgumentException(SR.Get(SRID.GlyphsClusterNoMatchingBracket));

                // look for colon separator
                int colon = valueSpec.IndexOf(':');
                if (colon == -1)
                {
                    // parse glyph cluster size
                    string characterClusterSpec = valueSpec.Substring(firstBracket + 1, secondBracket - (firstBracket + 1));
                    characterClusterSize = int.Parse(characterClusterSpec, CultureInfo.InvariantCulture);
                    glyphClusterSize = 1;
                }
                else
                {
                    if (colon <= firstBracket + 1 || colon >= secondBracket - 1)
                        throw new ArgumentException(SR.Get(SRID.GlyphsClusterMisplacedSeparator));
                    string characterClusterSpec = valueSpec.Substring(firstBracket + 1, colon - (firstBracket + 1));
                    characterClusterSize = int.Parse(characterClusterSpec, CultureInfo.InvariantCulture);
                    string glyphClusterSpec = valueSpec.Substring(colon + 1, secondBracket - (colon + 1));
                    glyphClusterSize = int.Parse(glyphClusterSpec, CultureInfo.InvariantCulture);
                }
                inCluster = true;
                glyphIndexString = valueSpec.Substring(secondBracket + 1);
            }
            if (IsEmpty(glyphIndexString))
                return false;

            glyphIndex = ushort.Parse(glyphIndexString, CultureInfo.InvariantCulture);
            return true;
        }

        private static double GetAdvanceWidth(GlyphTypeface glyphTypeface, ushort glyphIndex, bool sideways)
        {
            double advance = sideways ? glyphTypeface.AdvanceHeights[glyphIndex] : glyphTypeface.AdvanceWidths[glyphIndex];
            return advance * EmMultiplier;
        }

        private ushort GetGlyphFromCharacter(GlyphTypeface glyphTypeface, char character)
        {
            ushort glyphIndex;
            // TryGetValue will return zero glyph index for missing code points,
            // which is the right thing to display per http://www.microsoft.com/typography/otspec/cmap.htm
            glyphTypeface.CharacterToGlyphMap.TryGetValue(character, out glyphIndex);
            return glyphIndex;
        }

        /// <summary>
        /// Performs validation against cluster map size and throws a well defined exception.
        /// </summary>
        private static void SetClusterMapEntry(ushort[] clusterMap, int index, ushort value)
        {
            if (index < 0 || index >= clusterMap.Length)
                throw new ArgumentException(SR.Get(SRID.GlyphsUnicodeStringIsTooShort));
            clusterMap[index] = value;
        }

        private class ParsedGlyphData
        {
            public ushort   glyphIndex;
            public double   advanceWidth;
            public double   offsetX;
            public double   offsetY;
        };

        // -----------------------------------------------------------------------------
        // Parses a semicolon-delimited list of glyph specifiers, each of which consists
        // of up to 4 comma-delimited values:
        //   - glyph index (ushort)
        //   - glyph advance (double)
        //   - glyph offset X (double)
        //   - glyph offset Y (double)
        // A glyph entry can be have a cluster size prefix (int or pair of ints separated by a colon)
        // Whitespace adjacent to a delimiter (comma or semicolon) is ignored.
        // Returns the number of glyph specs parsed (number of semicolons plus 1).
        
        // Need to confirm the treatment of missing specifiers - the following code takes ""
        // to mean one glyph of all default values; ";" to mean two glyphs of all defaults;
        // "77,231;" to mean two glyphs, the second one all defaults. Right?
        private int ParseGlyphsProperty(
            GlyphTypeface               fontFace,
            string                      unicodeString,
            bool                        sideways,
            out List<ParsedGlyphData>   parsedGlyphs,
            out ushort[]                clusterMap)
        {
            string glyphsProp = Indices;

            // init for the whole parse, including the result arrays
            int parsedGlyphCount = 0;
            int parsedCharacterCount = 0;

            int characterClusterSize = 1;
            int glyphClusterSize = 1;

            bool inCluster = false;

            // make reasonable capacity guess on how many glyphs we can expect
            int estimatedNumberOfGlyphs;

            if (!String.IsNullOrEmpty(unicodeString))
            {
                clusterMap = new ushort[unicodeString.Length];
                estimatedNumberOfGlyphs = unicodeString.Length;
            }
            else
            {
                clusterMap = null;
                estimatedNumberOfGlyphs = 8;
            }

            if (!String.IsNullOrEmpty(glyphsProp))
                estimatedNumberOfGlyphs = Math.Max(estimatedNumberOfGlyphs, glyphsProp.Length / 5);

            parsedGlyphs = new List<ParsedGlyphData>(estimatedNumberOfGlyphs);

            ParsedGlyphData parsedGlyphData = new ParsedGlyphData();

            #region Parse Glyphs string
            if (!String.IsNullOrEmpty(glyphsProp))
            {
                // init per-glyph values for the first glyph/position
                int valueWithinGlyph = 0; // which value we're on (how many commas have we seen in this glyph)?
                int valueStartIndex = 0; // where (what index of Glyphs prop string) did this value start?

                // iterate and parse the characters of the Indices property
                for (int i = 0; i <= glyphsProp.Length; i++)
                {
                    // get next char or pseudo-terminator
                    char c = i < glyphsProp.Length ? glyphsProp[i] : '\0';

                    // finished scanning the current per-glyph value?
                    if ((c == ',') || (c == ';') || (i == glyphsProp.Length))
                    {
                        int len = i - valueStartIndex;

                        string valueSpec = glyphsProp.Substring(valueStartIndex, len);

                        #region Interpret one comma-delimited value

                        switch (valueWithinGlyph)
                        {
                            case 0:
                                bool wasInCluster = inCluster;
                                // interpret cluster size and glyph index spec
                                if (!ReadGlyphIndex(
                                    valueSpec,
                                    ref inCluster,
                                    ref glyphClusterSize,
                                    ref characterClusterSize,
                                    ref parsedGlyphData.glyphIndex))
                                {
                                    if (String.IsNullOrEmpty(unicodeString))
                                        throw new ArgumentException(SR.Get(SRID.GlyphsIndexRequiredIfNoUnicode));

                                    if (unicodeString.Length <= parsedCharacterCount)
                                        throw new ArgumentException(SR.Get(SRID.GlyphsUnicodeStringIsTooShort));

                                    parsedGlyphData.glyphIndex = GetGlyphFromCharacter(fontFace, unicodeString[parsedCharacterCount]);
                                }

                                if (!wasInCluster && clusterMap != null)
                                {
                                    // fill out cluster map at the start of each cluster
                                    if (inCluster)
                                    {
                                        for (int ch = parsedCharacterCount; ch < parsedCharacterCount + characterClusterSize; ++ch)
                                        {
                                            SetClusterMapEntry(clusterMap, ch, (ushort)parsedGlyphCount);
                                        }
                                    }
                                    else
                                    {
                                        SetClusterMapEntry(clusterMap, parsedCharacterCount, (ushort)parsedGlyphCount);
                                    }
                                }
                                parsedGlyphData.advanceWidth = GetAdvanceWidth(fontFace, parsedGlyphData.glyphIndex, sideways);
                                break;

                            case 1:
                                // interpret glyph advance spec
                                if (!IsEmpty(valueSpec))
                                {
                                    parsedGlyphData.advanceWidth = double.Parse(valueSpec, CultureInfo.InvariantCulture);
                                    if (parsedGlyphData.advanceWidth < 0)
                                        throw new ArgumentException(SR.Get(SRID.GlyphsAdvanceWidthCannotBeNegative));
                                }
                                break;

                            case 2:
                                // interpret glyph offset X
                                if (!IsEmpty(valueSpec))
                                    parsedGlyphData.offsetX = double.Parse(valueSpec, CultureInfo.InvariantCulture);
                                break;

                            case 3:
                                // interpret glyph offset Y
                                if (!IsEmpty(valueSpec))
                                    parsedGlyphData.offsetY = double.Parse(valueSpec, CultureInfo.InvariantCulture);
                                break;

                            default:
                                // too many commas; can't interpret
                                throw new ArgumentException(SR.Get(SRID.GlyphsTooManyCommas));
                        }
                        #endregion Interpret one comma-delimited value

                        // prepare to scan next value (if any)
                        valueWithinGlyph++;
                        valueStartIndex = i + 1;
                    }

                    // finished processing the current glyph?
                    if ((c == ';') || (i == glyphsProp.Length))
                    {
                        parsedGlyphs.Add(parsedGlyphData);
                        parsedGlyphData = new ParsedGlyphData();

                        if (inCluster)
                        {
                            --glyphClusterSize;
                            // when we reach the end of a glyph cluster, increment character index
                            if (glyphClusterSize == 0)
                            {
                                parsedCharacterCount += characterClusterSize;
                                inCluster = false;
                            }
                        }
                        else
                        {
                            ++parsedCharacterCount;
                        }
                        parsedGlyphCount++;

                        // initalize new per-glyph values
                        valueWithinGlyph = 0; // which value we're on (how many commas have we seen in this glyph)?
                        valueStartIndex = i + 1; // where (what index of Glyphs prop string) did this value start?
                    }
                }
            }
            #endregion

            // fill the remaining glyphs with defaults, assuming 1:1 mapping
            if (unicodeString != null)
            {
                while (parsedCharacterCount < unicodeString.Length)
                {
                    if (inCluster)
                        throw new ArgumentException(SR.Get(SRID.GlyphsIndexRequiredWithinCluster));

                    if (unicodeString.Length <= parsedCharacterCount)
                        throw new ArgumentException(SR.Get(SRID.GlyphsUnicodeStringIsTooShort));

                    parsedGlyphData.glyphIndex = GetGlyphFromCharacter(fontFace, unicodeString[parsedCharacterCount]);
                    parsedGlyphData.advanceWidth = GetAdvanceWidth(fontFace, parsedGlyphData.glyphIndex, sideways);
                    parsedGlyphs.Add(parsedGlyphData);
                    parsedGlyphData = new ParsedGlyphData();
                    SetClusterMapEntry(clusterMap, parsedCharacterCount, (ushort)parsedGlyphCount);
                    ++parsedCharacterCount;
                    ++parsedGlyphCount;
                }
            }

            // return number of glyphs actually specified
            return parsedGlyphCount;
        }
        #endregion Parsing and GlyphRun creation

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        private static void FillChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Called when Fill is changed.
            // If a SubPropertyInvalidatin is in progress this means a Freezable
            // has changed and we don't need to invalidate layout.  Otherwise
            // we have to invalidate

           ((UIElement)d).InvalidateVisual();
        }

        private static void GlyphRunPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Called when any property is changed that would require a new call to ParseGlyphRunProperties

            ((Glyphs)d)._glyphRunProperties = null;
        }

        private static void OriginPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Called when OriginX or OriginY is changed that would require recreation of the positioned GlyphRun
            // The _measurementGlyphRun will get updated as a result of layout
            ((Glyphs)d)._measurementGlyphRun = null;
        }

        /// <summary>
        /// Fill property
        /// </summary>
        public static readonly DependencyProperty FillProperty
            = DependencyProperty.Register(
                "Fill",
                typeof(Brush),
                typeof(Glyphs),
                new FrameworkPropertyMetadata(
                    (Brush)null,
                    FrameworkPropertyMetadataOptions.None,
                    new PropertyChangedCallback(FillChanged),
                    null)
                );

        /// <summary>
        /// Fill property
        /// </summary>
        public Brush Fill
        {
            get
            {
                return (Brush)GetValue(FillProperty);
            }
            set
            {
                SetValue(FillProperty, value);
            }
        }

        /// <summary>
        /// Indices property
        /// </summary>
        public static readonly DependencyProperty IndicesProperty =
            DependencyProperty.Register( "Indices", typeof(string), typeof(Glyphs),
                new FrameworkPropertyMetadata(string.Empty, 
                                              FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, 
                                              new PropertyChangedCallback(GlyphRunPropertyChanged)));

        /// <summary>
        /// Indices property accessor
        /// </summary>
        public string Indices
        {
            get
            {
                return (string)GetValue(IndicesProperty);
            }
            set
            {
                SetValue(IndicesProperty, value);
            }
        }

        /// <summary>
        /// UnicodeString property
        /// </summary>
        public static readonly DependencyProperty UnicodeStringProperty =
            DependencyProperty.Register( "UnicodeString", typeof(string), typeof(Glyphs),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, new PropertyChangedCallback(GlyphRunPropertyChanged)));

        /// <summary>
        /// UnicodeString property accessor
        /// </summary>
        public string UnicodeString
        {
            get
            {
                return (string)GetValue(UnicodeStringProperty);;
            }
            set
            {
                SetValue(UnicodeStringProperty, value);
            }
        }

        /// <summary>
        /// CaretStops property. The property syntax is a string of hexadecimal digits that describe an array of Boolean values that
        /// correspond to every code point in UnicodeString property.
        /// </summary>
        public static readonly DependencyProperty CaretStopsProperty =
            DependencyProperty.Register( "CaretStops", typeof(string), typeof(Glyphs),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsRender, new PropertyChangedCallback(GlyphRunPropertyChanged)));

        /// <summary>
        /// CaretStops property accessor
        /// </summary>
        public string CaretStops
        {
            get
            {
                return (string)GetValue(CaretStopsProperty);;
            }
            set
            {
                SetValue(CaretStopsProperty, value);
            }
        }

        /// <summary>
        /// FontRenderingEmSize property
        /// </summary>
        public static readonly DependencyProperty FontRenderingEmSizeProperty =
            DependencyProperty.Register( "FontRenderingEmSize", typeof(double), typeof(Glyphs),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, new PropertyChangedCallback(GlyphRunPropertyChanged)));

        /// <summary>
        /// FontRenderingEmSize property accessor
        /// </summary>
        [TypeConverter("System.Windows.FontSizeConverter, PresentationFramework, Version=" + BuildInfo.WCP_VERSION + ", Culture=neutral, PublicKeyToken=" + BuildInfo.WCP_PUBLIC_KEY_TOKEN + ", Custom=null")]
        public double FontRenderingEmSize
        {
            get
            {
                return (double)GetValue(FontRenderingEmSizeProperty);
            }
            set
            {
                SetValue(FontRenderingEmSizeProperty, value);
            }
        }

        /// <summary>
        /// OriginX property
        /// </summary>
        public static readonly DependencyProperty OriginXProperty =
            DependencyProperty.Register( "OriginX", typeof(double), typeof(Glyphs),
                new FrameworkPropertyMetadata(Double.NaN, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, new PropertyChangedCallback(OriginPropertyChanged)));

        /// <summary>
        /// OriginX property accessor
        /// </summary>
        [TypeConverter("System.Windows.LengthConverter, PresentationFramework, Version=" + BuildInfo.WCP_VERSION + ", Culture=neutral, PublicKeyToken=" + BuildInfo.WCP_PUBLIC_KEY_TOKEN + ", Custom=null")]
        public double OriginX
        {
            get
            {
                return (double)GetValue(OriginXProperty);
            }
            set
            {
                SetValue(OriginXProperty, value);
            }
        }

        /// <summary>
        /// OriginY property
        /// </summary>
        public static readonly DependencyProperty OriginYProperty =
            DependencyProperty.Register( "OriginY", typeof(double), typeof(Glyphs),
                new FrameworkPropertyMetadata(Double.NaN, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, new PropertyChangedCallback(OriginPropertyChanged)));

        /// <summary>
        /// OriginY property accessor
        /// </summary>
        [TypeConverter("System.Windows.LengthConverter, PresentationFramework, Version=" + BuildInfo.WCP_VERSION + ", Culture=neutral, PublicKeyToken=" + BuildInfo.WCP_PUBLIC_KEY_TOKEN + ", Custom=null")]
        public double OriginY
        {
            get
            {
                return (double)GetValue(OriginYProperty);
            }
            set
            {
                SetValue(OriginYProperty, value);
            }
        }

        /// <summary>
        /// FontUri property
        /// </summary>
        public static readonly DependencyProperty FontUriProperty =
            DependencyProperty.Register( "FontUri", typeof(Uri), typeof(Glyphs),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, new PropertyChangedCallback(GlyphRunPropertyChanged)));

        /// <summary>
        /// FontUri property accessor
        /// </summary>
        public Uri FontUri
        {
            get
            {
                return (Uri)GetValue(FontUriProperty);
            }
            set
            {
                SetValue(FontUriProperty, value);
            }
        }

        /// <summary>
        /// StyleSimulations property
        /// </summary>
        public static readonly DependencyProperty StyleSimulationsProperty =
            DependencyProperty.Register( "StyleSimulations", typeof(StyleSimulations), typeof(Glyphs),
                new FrameworkPropertyMetadata(StyleSimulations.None, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, new PropertyChangedCallback(GlyphRunPropertyChanged)));

        /// <summary>
        /// StyleSimulations property accessor
        /// </summary>
        public StyleSimulations StyleSimulations
        {
            get
            {
                return (StyleSimulations)GetValue(StyleSimulationsProperty);
            }
            set
            {
                SetValue(StyleSimulationsProperty, value);
            }
        }

        /// <summary>
        /// Sideways property
        /// </summary>
        public static readonly DependencyProperty IsSidewaysProperty =
            DependencyProperty.Register( "IsSideways", typeof(bool), typeof(Glyphs),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, new PropertyChangedCallback(GlyphRunPropertyChanged)));

        /// <summary>
        /// Specifies whether to rotate characters/glyphs 90 degrees anti-clockwise
        /// and use vertical baseline positioning metrics.
        /// </summary>
        /// <value>true if the rotation should be applied, false otherwise.</value>
        public bool  IsSideways
        {
            get
            {
                return (bool)GetValue(IsSidewaysProperty);
            }
            set
            {
                SetValue(IsSidewaysProperty, value);
            }
        }

        /// <summary>
        /// BidiLevel property
        /// </summary>
        public static readonly DependencyProperty BidiLevelProperty =
            DependencyProperty.Register( "BidiLevel", typeof(int), typeof(Glyphs),
                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, new PropertyChangedCallback(GlyphRunPropertyChanged)));

        /// <summary>
        /// Determines LTR/RTL reading order and bidi nesting.
        /// </summary>
        /// <value>The value of bidirectional nesting level.</value>
        public int BidiLevel
        {
            get
            {
                return (int)GetValue(BidiLevelProperty);
            }
            set
            {
                SetValue(BidiLevelProperty, value);
            }
        }

        /// <summary>
        /// DeviceFontName property
        /// </summary>
        public static readonly DependencyProperty DeviceFontNameProperty =
            DependencyProperty.Register("DeviceFontName", typeof(string), typeof(Glyphs),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, new PropertyChangedCallback(GlyphRunPropertyChanged)));

        /// <summary>
        /// Identifies a specific device font for which the Glyphs element has been optimized. When a Glyphs element is
        /// being rendered on a device that has built-in support for this named font, then the Glyphs element should be rendered using a
        /// possibly device specific mechanism for selecting that font, and by sending the Unicode codepoints rather than the
        /// glyph indices. When rendering onto a device that does not include built-in support for the named font,
        /// this property should be ignored.
        /// </summary>
        public string DeviceFontName
        {
            get
            {
                return (string) GetValue(DeviceFontNameProperty);
            }
            set
            {
                SetValue(DeviceFontNameProperty, value);
            }
        }

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        /// <summary>
        /// This property was added for performance reasons.  It allows D2 code
        /// to access the cached measurement glyph run instead of generating
        /// a new GlyphRun object by calling ToGlyphRun()
        /// </summary>
        internal GlyphRun MeasurementGlyphRun
        {
            get
            {
                if (_glyphRunProperties == null || _measurementGlyphRun == null)
                {
                    ComputeMeasurementGlyphRunAndOrigin();
                }
                return _measurementGlyphRun;
            }
        }
        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Classes
        //
        //------------------------------------------------------

        #region Private Classes

        /// <summary>
        /// This class is temporarily needed because GlyphRun includes rendering information
        /// that in future will be passed to DrawGlyphs separately.
        /// </summary>
        private class LayoutDependentGlyphRunProperties
        {
            public double           fontRenderingSize;
            public ushort []        glyphIndices;
            public double []        advanceWidths;
            public Point []         glyphOffsets;
            public ushort []        clusterMap;
            public bool             sideways;
            public int              bidiLevel;
            public GlyphTypeface    glyphTypeface;
            public string           unicodeString;
            public IList<bool>      caretStops;
            public string           deviceFontName;
            private float _pixelsPerDip;

            public LayoutDependentGlyphRunProperties(double pixelsPerDip)
            {
                _pixelsPerDip = (float)pixelsPerDip;
            }

            public GlyphRun CreateGlyphRun(Point origin, XmlLanguage language)
            {
                return new GlyphRun(
                    glyphTypeface,               // GlyphTypeface
                    bidiLevel,                   // Bidi level
                    sideways,                    // sideways flag
                    fontRenderingSize,           // rendering em size in MIL units
                    _pixelsPerDip,
                    glyphIndices,                // glyph indices
                    origin,                      // origin of glyph-drawing space
                    advanceWidths,               // glyph advances
                    glyphOffsets,                // glyph offsets
                    unicodeString.ToCharArray(), // unicode characters
                    deviceFontName,              // device font
                    clusterMap,                  // cluster map
                    caretStops,                  // caret stops
                    language                     // language
                );
            }
        }

        #endregion Private Classes

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        /// <summary>
        /// Caches the result of parsing GlyphRun properties.
        /// </summary>
        private LayoutDependentGlyphRunProperties   _glyphRunProperties;

        /// <summary>
        /// This GlyphRun instance is needed for measurement purposes only.
        /// </summary>
        private GlyphRun                            _measurementGlyphRun;

        private Point                               _glyphRunOrigin = new Point();
        private const double                        EmMultiplier = 100.0;

        #endregion Private Fields
    };
}

