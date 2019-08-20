// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
// Description: GlyphTypeface implementation
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;

using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Composition;
using System.Windows.Media.TextFormatting;
using System.Windows.Markup;

using MS.Internal;
using MS.Internal.TextFormatting;
using MS.Internal.FontCache;
using MS.Internal.FontFace;
using MS.Internal.PresentationCore;
using UnsafeNativeMethods=MS.Win32.PresentationCore.UnsafeNativeMethods;


namespace System.Windows.Media
{
    /// <summary>
    /// Physical font face corresponds to a font file on the disk
    /// </summary>
    public class GlyphTypeface : ITypefaceMetrics, ISupportInitialize
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Creates an uninitialized GlyphTypeface object. Caller should call ISupportInitialize.BeginInit()
        /// to begin initializing the object and call ISupportInitialize.EndInit() to finish the initialization.
        /// </summary>
        public GlyphTypeface()
        {
        }

        /// <summary>
        /// Creates a new GlyphTypeface object from a .otf, .ttf or .ttc font face specified by typefaceSource.
        /// The constructed GlyphTypeface does not use style simulations.
        /// </summary>
        /// <param name="typefaceSource">Specifies the URI of a font file used by the newly created GlyphTypeface.</param>
        public GlyphTypeface(Uri typefaceSource) : this(typefaceSource, StyleSimulations.None)
        {}

        /// <summary>
        /// Creates a new GlyphTypeface object from a .otf, .ttf or .ttc font face specified by typefaceSource.
        /// The constructed GlyphTypeface uses style simulations specified by styleSimulations parameter.
        /// </summary>
        /// <param name="typefaceSource">Specifies the URI of a font file used by the newly created GlyphTypeface.</param>
        /// <param name="styleSimulations">Specifies style simulations to be applied to the newly created GlyphTypeface.</param>
        public GlyphTypeface(Uri typefaceSource, StyleSimulations styleSimulations)
        {
            Initialize(typefaceSource, styleSimulations);
        }

        /// <summary>
        /// Creates a new GlyphTypeface object from a DWrite.Font object.
        /// </summary>
        /// <param name="font">The DWrite.Font object.</param>
        internal GlyphTypeface(MS.Internal.Text.TextInterface.Font font)
        {        
            StyleSimulations styleSimulations = (StyleSimulations)font.SimulationFlags;
            _font = font;

            string uriPath;

            MS.Internal.Text.TextInterface.FontFace fontFaceDWrite = _font.GetFontFace();
            try
            {
                using (MS.Internal.Text.TextInterface.FontFile fontFile = fontFaceDWrite.GetFileZero())
                {
                    uriPath = fontFile.GetUriPath();
                }

                // store the original Uri that contains the face index
                _originalUri = new SecurityCriticalDataClass<Uri>(Util.CombineUriWithFaceIndex(uriPath, checked((int)fontFaceDWrite.Index)));
            }
            finally
            {
                fontFaceDWrite.Release();
            }

            Uri typefaceSource = new Uri(uriPath);
           
            _fontFace = new FontFaceLayoutInfo(font);
            // We skip permission demands for FontSource because the above line already demands them for the right callers.
            _fontSource = new FontSource(typefaceSource, true);

            Invariant.Assert(  styleSimulations == StyleSimulations.None 
                            || styleSimulations == StyleSimulations.ItalicSimulation 
                            || styleSimulations == StyleSimulations.BoldSimulation
                            || styleSimulations == StyleSimulations.BoldItalicSimulation);
            
            _styleSimulations = styleSimulations;

            _initializationState = InitializationState.IsInitialized; // fully initialized
        }

        private void Initialize(Uri typefaceSource, StyleSimulations styleSimulations)
        {
            if (typefaceSource == null)
                throw new ArgumentNullException("typefaceSource");

            if (!typefaceSource.IsAbsoluteUri)
                throw new ArgumentException(SR.Get(SRID.UriNotAbsolute), "typefaceSource");

            // remember the original Uri that contains face index
            _originalUri = new SecurityCriticalDataClass<Uri>(typefaceSource);

            // split the Uri into the font source Uri and face index
            Uri fontSourceUri;
            int faceIndex;
            Util.SplitFontFaceIndex(typefaceSource, out fontSourceUri, out faceIndex);

            if (   styleSimulations != StyleSimulations.None 
                && styleSimulations != StyleSimulations.ItalicSimulation 
                && styleSimulations != StyleSimulations.BoldSimulation
                && styleSimulations != StyleSimulations.BoldItalicSimulation)
            {
                throw new InvalidEnumArgumentException("styleSimulations", (int)styleSimulations, typeof(StyleSimulations));
            }

            _styleSimulations = styleSimulations;

            MS.Internal.Text.TextInterface.FontCollection fontCollection = DWriteFactory.GetFontCollectionFromFile(fontSourceUri);
            using (MS.Internal.Text.TextInterface.FontFace fontFaceDWrite = DWriteFactory.Instance.CreateFontFace(fontSourceUri,
                                                                                                                  (uint)faceIndex,
                                                                                                                  (MS.Internal.Text.TextInterface.FontSimulations)styleSimulations))
            {
                // This is the same behavior as 3.*. If we pass, for example, a path to a composite font file then a 
                // FileFormatException will be thrown!
                if (fontFaceDWrite == null)
                {
                    throw new System.IO.FileFormatException(typefaceSource);             
                }
                _font = fontCollection.GetFontFromFontFace(fontFaceDWrite);
            }

            _fontFace = new FontFaceLayoutInfo(_font);

            // We skip permission demands for FontSource because the above line already demands them for the right callers.
            _fontSource = new FontSource(fontSourceUri, true);


            _initializationState = InitializationState.IsInitialized; // fully initialized
        }        

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// Return hash code for this GlyphTypeface.
        /// </summary>
        /// <returns>Hash code.</returns>
        public override int GetHashCode()
        {
            CheckInitialized();
            return _originalUri.Value.GetHashCode() ^ (int)StyleSimulations;
        }

        /// <summary>
        /// Compares this GlyphTypeface with another object.
        /// </summary>
        /// <param name="o">Object to compare with.</param>
        /// <returns>Whether this object is equal to the input object.</returns>
        public override bool Equals(object o)
        {
            CheckInitialized();
            GlyphTypeface t = o as GlyphTypeface;
            if (t == null)
                return false;

            return StyleSimulations == t.StyleSimulations
                && _originalUri.Value == t._originalUri.Value;
        }

        /// <summary>
        /// Returns a geometry describing the path for a single glyph in the font.
        /// The path represents the glyph
        /// without grid fitting applied for rendering at a specific resolution.
        /// </summary>
        /// <param name="glyphIndex">Index of the glyph to get outline for.</param>
        /// <param name="renderingEmSize">Font size in drawing surface units.</param>
        /// <param name="hintingEmSize">Size to hint for in points.</param>
        /// <returns></returns>
        [CLSCompliant(false)]
        public Geometry GetGlyphOutline(ushort glyphIndex, double renderingEmSize, double hintingEmSize)        
        {
            CheckInitialized(); // This can only be called on fully initialized GlyphTypeface
            // NOTE: This parameter is unused, and should be deleted. Not worth a breaking change just for this though.
            return ComputeGlyphOutline(glyphIndex, false, renderingEmSize);
        }

        /// <summary>
        /// Returns the binary image of font subset.
        /// </summary>
        /// <param name="glyphs">Collection of glyph indices to be included into the subset.</param>
        /// <returns>Binary image of font subset.</returns>
        /// <remarks>
        ///     Callers must have UnmanagedCode permission to call this API.
        ///     Callers must have FileIOPermission or WebPermission to font location to call this API.
        /// </remarks>
        [CLSCompliant(false)]
        public byte[] ComputeSubset(ICollection<ushort> glyphs)
        {
            CheckInitialized(); // This can only be called on fully initialized GlyphTypeface

            if (glyphs == null)
                throw new ArgumentNullException("glyphs");

            if (glyphs.Count <= 0)
                throw new ArgumentException(SR.Get(SRID.CollectionNumberOfElementsMustBeGreaterThanZero), "glyphs");

            if (glyphs.Count > ushort.MaxValue)
                throw new ArgumentException(SR.Get(SRID.CollectionNumberOfElementsMustBeLessOrEqualTo, ushort.MaxValue), "glyphs");

            UnmanagedMemoryStream pinnedFontSource = FontSource.GetUnmanagedStream();

            try
            {
                TrueTypeFontDriver trueTypeDriver = new TrueTypeFontDriver(pinnedFontSource, _originalUri.Value);
                trueTypeDriver.SetFace(FaceIndex);

                return trueTypeDriver.ComputeFontSubset(glyphs);
            }
            catch (SEHException e)
            {
                throw Util.ConvertInPageException(FontSource, e);
            }
            finally
            {
                pinnedFontSource.Close();
            }
        }

        /// <summary>
        /// Returns a font file stream represented by this GlyphTypeface.
        /// </summary>
        /// <returns>A font file stream represented by this GlyphTypeface.</returns>
        public Stream GetFontStream()
        {
            CheckInitialized(); // This can only be called on fully initialized GlyphTypeface
            return FontSource.GetStream();
        }

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// Returns the original Uri of this glyph typeface object.
        /// </summary>
        /// <value>The Uri glyph typeface was constructed with.</value>
        /// <remarks>
        ///     Callers must have FileIOPermission(FileIOPermissionAccess.PathDiscovery) for the given Uri to call this API.
        /// </remarks>
        public Uri FontUri
        {
            get
            {
                CheckInitialized(); // This can only be called on fully initialized GlyphTypeface
                return _originalUri.Value;
            }
            set
            {
                CheckInitializing(); // This can only be called in initialization

                if (value == null)
                    throw new ArgumentNullException("value");

                if (!value.IsAbsoluteUri)
                    throw new ArgumentException(SR.Get(SRID.UriNotAbsolute), "value");

                _originalUri = new SecurityCriticalDataClass<Uri>(value);
            }
        }

        /// <summary>
        /// This property is indexed by a Culture Identifier.
        /// It returns the family name in the specified language, or,
        /// if the font does not provide a name for the specified language,
        /// it returns the family name in English.
        /// The family name excludes weight, style and stretch.
        /// </summary>
        public IDictionary<CultureInfo,string> FamilyNames
        {
            get
            {
                CheckInitialized(); // This can only be called on fully initialized GlyphTypeface
                MS.Internal.Text.TextInterface.LocalizedStrings localizedStrings;
                if (
                   _font.GetInformationalStrings(MS.Internal.Text.TextInterface.InformationalStringID.PreferredFamilyNames, out localizedStrings)
                   ||
                   _font.GetInformationalStrings(MS.Internal.Text.TextInterface.InformationalStringID.WIN32FamilyNames, out localizedStrings)
                   )
                {
                    return localizedStrings;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// This property is indexed by a Culture Identifier.
        /// It returns the face name in the specified language, or,
        /// if the font does not provide a name for the specified language,
        /// it returns the face name in English.
        /// The face name may identify weight, style and/or stretch.
        /// </summary>
        public IDictionary<CultureInfo, string> FaceNames
        {
            get
            {
                CheckInitialized(); // This can only be called on fully initialized GlyphTypeface
                MS.Internal.Text.TextInterface.LocalizedStrings localizedStrings;
                if (
                   _font.GetInformationalStrings(MS.Internal.Text.TextInterface.InformationalStringID.PreferredSubFamilyNames, out localizedStrings)
                   ||
                   _font.GetInformationalStrings(MS.Internal.Text.TextInterface.InformationalStringID.Win32SubFamilyNames, out localizedStrings)
                   )
                {
                    return localizedStrings;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// This property is indexed by a Culture Identifier.
        /// It returns the family name in the specified language, or,
        /// if the font does not provide a name for the specified language,
        /// it returns the family name in English.
        /// The Win32FamilyName name excludes regular or bold weights and style,
        /// but includes other weights and stretch.
        /// </summary>
        public IDictionary<CultureInfo, string> Win32FamilyNames
        {
            get
            {
                CheckInitialized(); // This can only be called on fully initialized GlyphTypeface
                return GetFontInfo(MS.Internal.Text.TextInterface.InformationalStringID.WIN32FamilyNames);
            }
        }

        /// <summary>
        /// This property is indexed by a Culture Identifier.
        /// It returns the face name in the specified language, or,
        /// if the font does not provide a name for the specified language,
        /// it returns the face name in English.
        /// The face name may identify weight, style and/or stretch.
        /// </summary>
        IDictionary<XmlLanguage, string> ITypefaceMetrics.AdjustedFaceNames
        {
            get
            {
                CheckInitialized(); // This can only be called on fully initialized GlyphTypeface

                IDictionary<CultureInfo, string> adjustedFaceNames = _font.FaceNames;
                IDictionary<XmlLanguage, string> adjustedLanguageFaceNames = new Dictionary<XmlLanguage, string>(adjustedFaceNames.Count);

                foreach (KeyValuePair<CultureInfo, string> pair in adjustedFaceNames)
                {
                    adjustedLanguageFaceNames[XmlLanguage.GetLanguage(pair.Key.IetfLanguageTag)] = pair.Value;
                }

                return adjustedLanguageFaceNames;
            }
        }

        /// <summary>
        /// This property is indexed by a Culture Identifier.
        /// It returns the face name in the specified language, or,
        /// if the font does not provide a name for the specified language,
        /// it returns the face name in English.
        /// The Win32Face name may identify weights other than regular or bold and/or style,
        /// but may not identify stretch or other weights.
        /// </summary>
        public IDictionary<CultureInfo, string> Win32FaceNames
        {
            get
            {
                CheckInitialized(); // This can only be called on fully initialized GlyphTypeface
                return GetFontInfo(MS.Internal.Text.TextInterface.InformationalStringID.Win32SubFamilyNames);
            }
        }

        /// <summary>
        /// This property is indexed by a Culture Identifier.
        /// Version string in the fonts NAME table.
        /// Version strings vary significantly in format - to obtain the version
        /// as a numeric value use the 'Version' property,
        /// do not attempt to parse the VersionString.
        /// </summary>
        public IDictionary<CultureInfo, string> VersionStrings
        {
            get
            {
                CheckInitialized(); // This can only be called on fully initialized GlyphTypeface
                return GetFontInfo(MS.Internal.Text.TextInterface.InformationalStringID.VersionStrings);
            }
        }

        /// <summary>
        /// This property is indexed by a Culture Identifier.
        /// Copyright notice.
        /// </summary>
        public IDictionary<CultureInfo, string> Copyrights
        {
            get
            {
                CheckInitialized(); // This can only be called on fully initialized GlyphTypeface
                return GetFontInfo(MS.Internal.Text.TextInterface.InformationalStringID.CopyrightNotice);
            }
        }

        /// <summary>
        /// This property is indexed by a Culture Identifier.
        /// Manufacturer Name.
        /// </summary>
        public IDictionary<CultureInfo, string> ManufacturerNames
        {
            get
            {
                CheckInitialized(); // This can only be called on fully initialized GlyphTypeface
                return GetFontInfo(MS.Internal.Text.TextInterface.InformationalStringID.Manufacturer);
            }
        }

        /// <summary>
        /// This property is indexed by a Culture Identifier.
        /// This is used to save any trademark notice/information for this font.
        /// Such information should be based on legal advice.
        /// This is distinctly separate from the copyright.
        /// </summary>
        public IDictionary<CultureInfo, string> Trademarks
        {
            get
            {
                CheckInitialized(); // This can only be called on fully initialized GlyphTypeface
                return GetFontInfo(MS.Internal.Text.TextInterface.InformationalStringID.Trademark);
            }
        }

        /// <summary>
        /// This property is indexed by a Culture Identifier.
        /// Name of the designer of the typeface.
        /// </summary>
        public IDictionary<CultureInfo, string> DesignerNames
        {
            get
            {
                CheckInitialized(); // This can only be called on fully initialized GlyphTypeface
                return GetFontInfo(MS.Internal.Text.TextInterface.InformationalStringID.Designer);
            }
        }

        /// <summary>
        /// This property is indexed by a Culture Identifier.
        /// Description of the typeface. Can contain revision information,
        /// usage recommendations, history, features, etc.
        /// </summary>
        public IDictionary<CultureInfo, string> Descriptions
        {
            get
            {
                CheckInitialized(); // This can only be called on fully initialized GlyphTypeface
                return GetFontInfo(MS.Internal.Text.TextInterface.InformationalStringID.Description);
            }
        }

        /// <summary>
        /// This property is indexed by a Culture Identifier.
        /// URL of font vendor (with protocol, e.g., `http://, `ftp://).
        /// If a unique serial number is embedded in the URL,
        /// it can be used to register the font.
        /// </summary>
        public IDictionary<CultureInfo, string> VendorUrls
        {
            get
            {
                CheckInitialized(); // This can only be called on fully initialized GlyphTypeface
                return GetFontInfo(MS.Internal.Text.TextInterface.InformationalStringID.FontVendorURL);
            }
        }

        /// <summary>
        /// This property is indexed by a Culture Identifier.
        /// URL of typeface designer (with protocol, e.g., `http://, `ftp://).
        /// </summary>
        public IDictionary<CultureInfo, string> DesignerUrls
        {
            get
            {
                CheckInitialized(); // This can only be called on fully initialized GlyphTypeface
                return GetFontInfo(MS.Internal.Text.TextInterface.InformationalStringID.DesignerURL);
            }
        }

        /// <summary>
        /// This property is indexed by a Culture Identifier.
        /// Description of how the font may be legally used,
        /// or different example scenarios for licensed use.
        /// This field should be written in plain language, not legalese.
        /// </summary>
        public IDictionary<CultureInfo, string> LicenseDescriptions
        {
            get
            {
                CheckInitialized(); // This can only be called on fully initialized GlyphTypeface
                return GetFontInfo(MS.Internal.Text.TextInterface.InformationalStringID.LicenseDescription);
            }
        }

        /// <summary>
        /// This property is indexed by a Culture Identifier.
        /// This can be the font name, or any other text that the designer
        /// thinks is the best sample to display the font in.
        /// </summary>
        public IDictionary<CultureInfo, string> SampleTexts
        {
            get
            {
                CheckInitialized(); // This can only be called on fully initialized GlyphTypeface
                return GetFontInfo(MS.Internal.Text.TextInterface.InformationalStringID.SampleText);
            }
        }

        /// <summary>
        /// Returns designed style (regular/italic/oblique) of this font face
        /// </summary>
        /// <value>Designed style of this font face.</value>
        public FontStyle Style
        {
            get
            {
                CheckInitialized(); // This can only be called on fully initialized GlyphTypeface
                return new FontStyle((int)_font.Style);
            }
        }

        /// <summary>
        /// Returns designed weight of this font face.
        /// </summary>
        /// <value>Designed weight of this font face.</value>
        public FontWeight Weight
        {
            get
            {
                CheckInitialized(); // This can only be called on fully initialized GlyphTypeface
                return new FontWeight((int)_font.Weight);
            }
        }

        /// <summary>
        /// Returns designed stretch of this font face.
        /// </summary>
        /// <value>Designed stretch of this font face.</value>
        public FontStretch Stretch
        {
            get
            {
                CheckInitialized(); // This can only be called on fully initialized GlyphTypeface
                return new FontStretch((int)_font.Stretch);
            }
        }

        /// <summary>
        /// Font face version interpreted from the font's 'NAME' table.
        /// </summary>
        public double Version
        {
            get
            {
                CheckInitialized(); // This can only be called on fully initialized GlyphTypeface
                return _font.Version;
            }
        }

        /// <summary>
        /// Height of character cell relative to em size.
        /// </summary>
        public double Height
        {
            get
            {
                CheckInitialized(); // This can only be called on fully initialized GlyphTypeface
                return (double)(_font.Metrics.Ascent + _font.Metrics.Descent) / _font.Metrics.DesignUnitsPerEm;
            }
        }

        /// <summary>
        /// Distance from cell top to English baseline relative to em size.
        /// </summary>
        public double Baseline
        {
            get
            {
                CheckInitialized(); // This can only be called on fully initialized GlyphTypeface
                return (double)_font.Metrics.Ascent / _font.Metrics.DesignUnitsPerEm;
            }
        }

        /// <summary>
        /// Distance from baseline to top of English capital relative to em size.
        /// </summary>
        public double CapsHeight
        {
            get
            {
                CheckInitialized(); // This can only be called on fully initialized GlyphTypeface
                return (double)_font.Metrics.CapHeight / _font.Metrics.DesignUnitsPerEm;
            }
        }

        /// <summary>
        /// Western x-height relative to em size.
        /// </summary>
        public double XHeight
        {
            get
            {
                CheckInitialized(); // This can only be called on fully initialized GlyphTypeface
                return (double)_font.Metrics.XHeight / _font.Metrics.DesignUnitsPerEm;
            }
        }

        /// <summary>
        /// Returns true if this font does not conform to Unicode encoding:
        /// it may be considered as a simple collection of symbols indexed by a codepoint.
        /// </summary>
        public bool Symbol
        {
            get
            {
                CheckInitialized(); // This can only be called on fully initialized GlyphTypeface
                return _font.IsSymbolFont;
            }
        }

        /// <summary>
        /// Position of underline relative to baseline relative to em size.
        /// The value is usually negative, to place the underline below the baseline.
        /// </summary>
        public double UnderlinePosition
        {
            get
            {
                CheckInitialized(); // This can only be called on fully initialized GlyphTypeface
                return (double)_font.Metrics.UnderlinePosition / _font.Metrics.DesignUnitsPerEm;
            }
        }

        /// <summary>
        /// Thickness of underline relative to em size.
        /// </summary>
        public double UnderlineThickness
        {
            get
            {
                CheckInitialized(); // This can only be called on fully initialized GlyphTypeface
                return (double)_font.Metrics.UnderlineThickness / _font.Metrics.DesignUnitsPerEm;
            }
        }

        /// <summary>
        /// Position of strikeThrough relative to baseline relative to em size.
        /// The value is usually positive, to place the Strikethrough above the baseline.
        /// </summary>
        public double StrikethroughPosition
        {
            get
            {
                CheckInitialized(); // This can only be called on fully initialized GlyphTypeface
                return (double)_font.Metrics.StrikethroughPosition / _font.Metrics.DesignUnitsPerEm;
            }
        }

        /// <summary>
        /// Thickness of Strikethrough relative to em size.
        /// </summary>
        public double StrikethroughThickness
        {
            get
            {
                CheckInitialized(); // This can only be called on fully initialized GlyphTypeface
                return (double)_font.Metrics.StrikethroughThickness / _font.Metrics.DesignUnitsPerEm;
            }
        }

        /// <summary>
        /// EmbeddingRights property describes font embedding permissions
        /// specified in this glyph typeface.
        /// </summary>
        public FontEmbeddingRight EmbeddingRights
        {
            get
            {
                CheckInitialized(); // This can only be called on fully initialized GlyphTypeface
                return _fontFace.EmbeddingRights;
            }
        }

        #region ITypefaceMetrics implementation

        /// <summary>
        /// Distance from baseline to top of English capital, relative to em size.
        /// </summary>
        double ITypefaceMetrics.CapsHeight
        {
            get
            {
                return CapsHeight;
            }
        }

        /// <summary>
        /// Western x-height relative to em size.
        /// </summary>
        double ITypefaceMetrics.XHeight
        {
            get
            {
                return XHeight;
            }
        }

        /// <summary>
        /// Returns true if this font does not conform to Unicode encoding:
        /// it may be considered as a simple collection of symbols indexed by a codepoint.
        /// </summary>
        bool ITypefaceMetrics.Symbol
        {
            get
            {
                return Symbol;
            }
        }

        /// <summary>
        /// Position of underline relative to baseline relative to em size.
        /// The value is usually negative, to place the underline below the baseline.
        /// </summary>
        double ITypefaceMetrics.UnderlinePosition
        {
            get
            {
                return UnderlinePosition;
            }
        }

        /// <summary>
        /// Thickness of underline relative to em size.
        /// </summary>
        double ITypefaceMetrics.UnderlineThickness
        {
            get
            {
                return UnderlineThickness;
            }
        }

        /// <summary>
        /// Position of strikeThrough relative to baseline relative to em size.
        /// The value is usually positive, to place the Strikethrough above the baseline.
        /// </summary>
        double ITypefaceMetrics.StrikethroughPosition
        {
            get
            {
                return StrikethroughPosition;
            }
        }

        /// <summary>
        /// Thickness of Strikethrough relative to em size.
        /// </summary>
        double ITypefaceMetrics.StrikethroughThickness
        {
            get
            {
                return StrikethroughThickness;
            }
        }

        #endregion


        // The next several properties return non CLS-compliant types.
        // For now, suppress the compiler warning.

#pragma warning disable 3003

        /// <summary>
        /// Returns advance width for a given glyph.
        /// </summary>
        public IDictionary<ushort, double> AdvanceWidths
        {
            get
            {
                CheckInitialized(); // This can only be called on fully initialized GlyphTypeface
                return CreateGlyphIndexer(this.GetAdvanceWidth);
            }
        }

        /// <summary>
        /// Returns Advance height for a given glyph (Used for example in vertical layout).
        /// </summary>
        public IDictionary<ushort, double> AdvanceHeights
        {
            get
            {
                CheckInitialized(); // This can only be called on fully initialized GlyphTypeface
                return CreateGlyphIndexer(this.GetAdvanceHeight);
            }
        }

        /// <summary>
        /// Distance from leading end of advance vector to left edge of black box.
        /// Positive when left edge of black box is within the alignment rectangle
        /// defined by the advance width and font cell height.
        /// Negative when left edge of black box overhangs the alignment rectangle.
        /// </summary>
        public IDictionary<ushort, double> LeftSideBearings
        {
            get
            {
                CheckInitialized(); // This can only be called on fully initialized GlyphTypeface
                return CreateGlyphIndexer(this.GetLeftSidebearing);
            }
        }

        /// <summary>
        /// Distance from right edge of black box to right end of advance vector.
        /// Positive when trailing edge of black box is within the alignment rectangle
        /// defined by the advance width and font cell height.
        /// Negative when right edge of black box overhangs the alignment rectangle.
        /// </summary>
        public IDictionary<ushort, double> RightSideBearings
        {
            get
            {
                CheckInitialized(); // This can only be called on fully initialized GlyphTypeface
                return CreateGlyphIndexer(this.GetRightSidebearing);
            }
        }

        /// <summary>
        /// Distance from top end of (vertical) advance vector to top edge of black box.
        /// Positive when top edge of black box is within the alignment rectangle
        /// defined by the advance height and font cell height.
        /// (The font cell height is a horizontal dimension in vertical layout).
        /// Negative when top edge of black box overhangs the alignment rectangle.
        /// </summary>
        public IDictionary<ushort, double> TopSideBearings
        {
            get
            {
                CheckInitialized(); // This can only be called on fully initialized GlyphTypeface
                return CreateGlyphIndexer(this.GetTopSidebearing);
            }
        }

        /// <summary>
        /// Distance from bottom edge of black box to bottom end of advance vector.
        /// Positive when bottom edge of black box is within the alignment rectangle
        /// defined by the advance width and font cell height.
        /// (The font cell height is a horizontal dimension in vertical layout).
        /// Negative when bottom edge of black box overhangs the alignment rectangle.
        /// </summary>
        public IDictionary<ushort, double> BottomSideBearings
        {
            get
            {
                CheckInitialized(); // This can only be called on fully initialized GlyphTypeface
                return CreateGlyphIndexer(this.GetBottomSidebearing);
            }
        }

        /// <summary>
        /// Offset down from horizontal Western baseline to bottom  of glyph black box.
        /// </summary>
        public IDictionary<ushort, double> DistancesFromHorizontalBaselineToBlackBoxBottom
        {
            get
            {
                CheckInitialized(); // This can only be called on fully initialized GlyphTypeface
                return CreateGlyphIndexer(this.GetBaseline);
            }
        }

        /// <summary>
        /// Returns nominal mapping of Unicode codepoint to glyph index as defined by the font 'CMAP' table.
        /// </summary>
        public IDictionary<int, ushort> CharacterToGlyphMap
        {
            get
            {
                CheckInitialized(); // This can only be called on fully initialized GlyphTypeface
                return _fontFace.CharacterMap;
            }
        }

        #pragma warning restore 3003

        /// <summary>
        /// Returns algorithmic font style simulations to be applied to the GlyphTypeface.
        /// </summary>
        public StyleSimulations StyleSimulations
        {
            get
            {
                CheckInitialized(); // This can only be called on fully initialized GlyphTypeface
                return _styleSimulations;
            }
            set
            {
                CheckInitializing();
                _styleSimulations = value;
            }
        }

        /// <summary>
        /// Obtains the number of glyphs in the glyph typeface.
        /// </summary>
        /// <value>The number of glyphs in the glyph typeface.</value>
        public int GlyphCount
        {
            get
            {
                CheckInitialized(); // This can only be called on fully initialized GlyphTypeface
                int glyphCount;

                MS.Internal.Text.TextInterface.FontFace fontFaceDWrite = _font.GetFontFace();
                try
                {
                    glyphCount = fontFaceDWrite.GlyphCount;
                }
                finally
                {
                    fontFaceDWrite.Release();
                }

                return glyphCount;
            }
        }

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        internal bool HasCharacter(uint unicodeValue)
        {
            return FontDWrite.HasCharacter(unicodeValue);
        }
        
        internal MS.Internal.Text.TextInterface.Font FontDWrite
        {
            get
            {                
                CheckInitialized(); // This can only be called on fully initialized GlyphTypeface
                return _font;
            }
        }

        /// <summary>
        /// Returns the nominal advance width for a glyph.
        ///
        /// PERFORMANCE WARNING: This function will make a call through the MC++ interop layer
        /// into DWrite. If called repetitively the call overhead can be significant. If you have
        /// a lot of glyphs you need to get metrics for, use the array based equivalent instead.
        ///
        /// </summary>
        /// <param name="glyph">Glyph index in the font.</param>
        /// <returns>The nominal advance width for the glyph relative to the em size of the font.</returns>
        internal double GetAdvanceWidth(ushort             glyph,
                                        float              pixelsPerDip,
                                        TextFormattingMode textFormattingMode,
                                        bool               isSideways)
        {
            CheckInitialized(); // This can only be called on fully initialized GlyphTypeface

            // We manually expand GetGlyphMetrics call because GetAdvanceWidth is a very frequently used function.
            // When we get to using GetAdvanceHeight for vertical writing, we need to consider doing the same optimization there.
            unsafe
            {
                MS.Internal.Text.TextInterface.GlyphMetrics glyphMetrics = GlyphMetrics(glyph, DesignEmHeight, pixelsPerDip, textFormattingMode, isSideways);

                return (double)glyphMetrics.AdvanceWidth / DesignEmHeight;
            }
        }

        private double GetAdvanceHeight(ushort glyph,
            float pixelsPerDip,
            TextFormattingMode textFormattingMode,
            bool isSideways)
        {
            double aw, ah, lsb, rsb, tsb, bsb, baseline;
            GetGlyphMetrics(
                glyph,
                1.0,
                1.0,
                pixelsPerDip,
                textFormattingMode,
                isSideways,
                out aw,
                out ah,
                out lsb,
                out rsb,
                out tsb,
                out bsb,
                out baseline
            );
            return ah;
        }

        private unsafe MS.Internal.Text.TextInterface.GlyphMetrics GlyphMetrics(ushort             glyphIndex,
                                                                                double             emSize,
                                                                                float              pixelsPerDip,
                                                                                TextFormattingMode textFormattingMode,
                                                                                bool               isSideways)
        {
            MS.Internal.Text.TextInterface.GlyphMetrics glyphMetrics;

            MS.Internal.Text.TextInterface.FontFace fontFaceDWrite = _font.GetFontFace();
            try
            {
                if (glyphIndex >= fontFaceDWrite.GlyphCount)
                    throw new ArgumentOutOfRangeException("glyphIndex", SR.Get(SRID.GlyphIndexOutOfRange, glyphIndex));

                glyphMetrics = new MS.Internal.Text.TextInterface.GlyphMetrics();

                if (textFormattingMode == TextFormattingMode.Ideal)
                {
                    // We can safely pass pointers to glyphIndex and glyphMetrics since both are value types and are allocated on the stack.
                    fontFaceDWrite.GetDesignGlyphMetrics(&glyphIndex, 1, &glyphMetrics);
                }
                else
                {
                    // We can safely pass pointers to glyphIndex and glyphMetrics since both are value types and are allocated on the stack.
                    fontFaceDWrite.GetDisplayGlyphMetrics(&glyphIndex, 1, &glyphMetrics, checked((float)emSize),
                        textFormattingMode != TextFormattingMode.Display, isSideways, pixelsPerDip);
                }
            }
            finally
            {
                fontFaceDWrite.Release();
            }

            return glyphMetrics;
        }

        private unsafe void GlyphMetrics(ushort* pGlyphIndices, int characterCount, MS.Internal.Text.TextInterface.GlyphMetrics* pGlyphMetrics, double emSize, 
            float pixelsPerDip, TextFormattingMode textFormattingMode, bool isSideways)
        {
            MS.Internal.Text.TextInterface.FontFace fontFaceDWrite = _font.GetFontFace();
            try
            {
                if (textFormattingMode == TextFormattingMode.Ideal)
                {
                    fontFaceDWrite.GetDesignGlyphMetrics(pGlyphIndices, checked((uint)characterCount), pGlyphMetrics);
                }
                else
                {
                    fontFaceDWrite.GetDisplayGlyphMetrics(pGlyphIndices, checked((uint)characterCount), pGlyphMetrics, 
                        checked((float)emSize), textFormattingMode != TextFormattingMode.Display, isSideways, pixelsPerDip);
                }
            }
            finally
            {
                fontFaceDWrite.Release();
            }
        }

        private double GetLeftSidebearing(ushort         glyph,
                                          float          pixelsPerDip,
                                          TextFormattingMode textFormattingMode,
                                          bool           isSideways)
        {
            return ((double)GlyphMetrics(glyph, DesignEmHeight, pixelsPerDip, textFormattingMode, isSideways).LeftSideBearing) / DesignEmHeight;
        }

        private double GetRightSidebearing(ushort         glyph,
                                           float          pixelsPerDip,
                                           TextFormattingMode textFormattingMode,
                                           bool           isSideways)
        {
            return ((double)GlyphMetrics(glyph, DesignEmHeight, pixelsPerDip, textFormattingMode, isSideways).RightSideBearing) / DesignEmHeight;
        }

        private double GetTopSidebearing(ushort         glyph,
                                         float          pixelsPerDip,
                                         TextFormattingMode textFormattingMode,
                                         bool           isSideways)
        {
            return ((double)GlyphMetrics(glyph, DesignEmHeight, pixelsPerDip, textFormattingMode, isSideways).TopSideBearing) / DesignEmHeight;
        }

        private double GetBottomSidebearing(ushort         glyph,
                                            float          pixelsPerDip,
                                            TextFormattingMode textFormattingMode,
                                            bool           isSideways)
        {
            return ((double)GlyphMetrics(glyph, DesignEmHeight, pixelsPerDip, textFormattingMode, isSideways).BottomSideBearing) / DesignEmHeight;
        }

        private double GetBaseline(ushort glyph,
                                   float pixelsPerDip,
                                   TextFormattingMode textFormattingMode,
                                   bool isSideways)
        {
            MS.Internal.Text.TextInterface.GlyphMetrics glyphMetrics = GlyphMetrics(glyph, DesignEmHeight, pixelsPerDip, textFormattingMode, isSideways);
            return BaselineHelper(glyphMetrics) / DesignEmHeight;
        }

        internal static double BaselineHelper(MS.Internal.Text.TextInterface.GlyphMetrics metrics)
        {
            return  -1 * ((double)metrics.BottomSideBearing + metrics.VerticalOriginY - metrics.AdvanceHeight);
        }

        /// <summary>
        /// Optimized version of obtaining all of glyph metrics from font cache at once
        /// without repeated checks and divisions.
        ///
        /// PERFORMANCE WARNING: This function will make a call through the MC++ interop layer
        /// into DWrite. If called repetitively the call overhead can be significant. If you have
        /// a lot of glyphs you need to get metrics for, use the array based equivalent instead.
        ///
        /// </summary>
        internal void GetGlyphMetrics(
            ushort glyph,
            double renderingEmSize,
            double scalingFactor,
            float pixelsPerDip,
            TextFormattingMode textFormattingMode,
            bool isSideways,
            out double aw,
            out double ah,
            out double lsb,
            out double rsb,
            out double tsb,
            out double bsb,
            out double baseline
            )
        {
            CheckInitialized(); // This can only be called on fully initialized GlyphTypeface

            unsafe
            {
                MS.Internal.Text.TextInterface.GlyphMetrics glyphMetrics = GlyphMetrics(glyph, renderingEmSize, pixelsPerDip, textFormattingMode, isSideways);

                double designToEm = renderingEmSize / DesignEmHeight;

                if (TextFormattingMode.Display == textFormattingMode)
                {
                    aw = TextFormatterImp.RoundDipForDisplayMode(designToEm * glyphMetrics.AdvanceWidth, pixelsPerDip) * scalingFactor;
                    ah = TextFormatterImp.RoundDipForDisplayMode(designToEm * glyphMetrics.AdvanceHeight, pixelsPerDip) * scalingFactor;
                    lsb = TextFormatterImp.RoundDipForDisplayMode(designToEm * glyphMetrics.LeftSideBearing, pixelsPerDip) * scalingFactor;
                    rsb = TextFormatterImp.RoundDipForDisplayMode(designToEm * glyphMetrics.RightSideBearing, pixelsPerDip) * scalingFactor;
                    tsb = TextFormatterImp.RoundDipForDisplayMode(designToEm * glyphMetrics.TopSideBearing, pixelsPerDip) * scalingFactor;
                    bsb = TextFormatterImp.RoundDipForDisplayMode(designToEm * glyphMetrics.BottomSideBearing, pixelsPerDip) * scalingFactor;
                    baseline = TextFormatterImp.RoundDipForDisplayMode(designToEm * BaselineHelper(glyphMetrics), pixelsPerDip) * scalingFactor;
                }
                else
                {
                    aw = designToEm * glyphMetrics.AdvanceWidth * scalingFactor;
                    ah = designToEm * glyphMetrics.AdvanceHeight * scalingFactor;
                    lsb = designToEm * glyphMetrics.LeftSideBearing * scalingFactor;
                    rsb = designToEm * glyphMetrics.RightSideBearing * scalingFactor;
                    tsb = designToEm * glyphMetrics.TopSideBearing * scalingFactor;
                    bsb = designToEm * glyphMetrics.BottomSideBearing * scalingFactor;
                    baseline = designToEm * BaselineHelper(glyphMetrics) * scalingFactor;
                }
            }
        }

        /// <summary>
        /// Optimized version of obtaining all of glyph metrics from font cache at once
        /// without repeated checks and divisions.
        /// </summary>
        internal void GetGlyphMetrics(
            ushort[] glyphs,
            int glyphsLength,
            double renderingEmSize,
            float pixelsPerDip,
            TextFormattingMode textFormattingMode,
            bool isSideways,
            MS.Internal.Text.TextInterface.GlyphMetrics[] glyphMetrics
            )
        {
            CheckInitialized(); // This can only be called on fully initialized GlyphTypeface
            
            Invariant.Assert(glyphsLength <= glyphs.Length);

            unsafe
            {
                fixed (MS.Internal.Text.TextInterface.GlyphMetrics* pGlyphMetrics = glyphMetrics)
                {
                    fixed (ushort* pGlyphs = &glyphs[0])
                    {
                        GlyphMetrics(pGlyphs, glyphsLength, pGlyphMetrics, renderingEmSize, pixelsPerDip, textFormattingMode, isSideways);
                    }
                }
            }
        }        

        /// <summary>
        /// Returns a geometry describing the path for a single glyph in the font.
        /// The path represents the glyph
        /// without grid fitting applied for rendering at a specific resolution.
        /// </summary>
        /// <param name="glyphIndex">Index of the glyph to get outline for.</param>
        /// <param name="sideways">Specifies whether the glyph should be rotated sideways.</param>
        /// <param name="renderingEmSize">Font size in drawing surface units.</param>
        /// <returns>Geometry containing glyph outline.</returns>
        internal Geometry ComputeGlyphOutline(ushort glyphIndex,
                                              bool sideways,
                                              double renderingEmSize)
        {
            CheckInitialized();

            unsafe
            {
                byte* pMilPathGeometry;
                UInt32 size;
                FillRule fillRule;

                MS.Internal.Text.TextInterface.FontFace fontFaceDWrite = _font.GetFontFace();
                try
                {
                    HRESULT.Check(UnsafeNativeMethods.MilCoreApi.MilGlyphRun_GetGlyphOutline(
                        fontFaceDWrite.DWriteFontFaceAddRef, // Released in this native code function
                        glyphIndex,
                        sideways,
                        renderingEmSize,
                        out pMilPathGeometry,
                        out size,
                        out fillRule
                        ));
                }
                finally
                {
                    fontFaceDWrite.Release();
                }

                Geometry.PathGeometryData pathGeoData = new Geometry.PathGeometryData();
                byte[] data = new byte[size];
                Marshal.Copy(new IntPtr(pMilPathGeometry), data, 0, checked((int)size));
                
                // Delete the memory we allocated in native code.
                HRESULT.Check(UnsafeNativeMethods.MilCoreApi.MilGlyphRun_ReleasePathGeometryData(
                    pMilPathGeometry
                    ));
                
                pathGeoData.SerializedData = data;
                pathGeoData.FillRule = fillRule;
                pathGeoData.Matrix = CompositionResourceManager.MatrixToMilMatrix3x2D(Matrix.Identity);

                PathStreamGeometryContext ctx = new PathStreamGeometryContext(fillRule, null);
                PathGeometry.ParsePathGeometryData(pathGeoData, ctx);
    
                return ctx.GetPathGeometry();
            }
        }

        /// <summary>
        /// Get advance widths of unshaped characters
        /// </summary>
        /// <param name="unsafeCharString">character string</param>
        /// <param name="stringLength">character length</param>
        /// <param name="emSize">character em size</param>
        /// <param name="scalingFactor">This is the factor by which we will scale up 
        /// the metrics. Typically this value to used to convert metrics from the real 
        /// space to the ideal space</param>
        /// <param name="advanceWidthsUnshaped">unshaped advance widths </param>
        /// <param name="nullFont">true if all characters map to missing glyph</param>
        /// <returns>array of character advance widths</returns>
        internal unsafe void GetAdvanceWidthsUnshaped(
            char*              unsafeCharString,
            int                stringLength,
            double             emSize,
            float              pixelsPerDip,
            double             scalingFactor,
            int*               advanceWidthsUnshaped,
            bool               nullFont,
            TextFormattingMode textFormattingMode,
            bool               isSideways
            )
        {
            CheckInitialized(); // This can only be called on fully initialized GlyphTypeface
            Invariant.Assert(stringLength > 0);

            if (!nullFont)
            {
                CharacterBufferRange charBufferRange = new CharacterBufferRange(unsafeCharString, stringLength);
                MS.Internal.Text.TextInterface.GlyphMetrics[] glyphMetrics = BufferCache.GetGlyphMetrics(stringLength);

                GetGlyphMetricsOptimized(charBufferRange,
                                         emSize,
                                         pixelsPerDip,
                                         textFormattingMode,
                                         isSideways,
                                         glyphMetrics
                                         );

                if (TextFormattingMode.Display == textFormattingMode)
                {
                    double designToEm = emSize / DesignEmHeight;
                    for (int i = 0; i < stringLength; i++)
                    {
                        advanceWidthsUnshaped[i] = (int)Math.Round(TextFormatterImp.RoundDipForDisplayMode(glyphMetrics[i].AdvanceWidth * designToEm, pixelsPerDip) * scalingFactor);
                    }
                }
                else
                {
                    double designToEm = emSize * scalingFactor / DesignEmHeight;
                    for (int i = 0; i < stringLength; i++)
                    {
                        advanceWidthsUnshaped[i] = (int)Math.Round(glyphMetrics[i].AdvanceWidth * designToEm);
                    }
                }

                BufferCache.ReleaseGlyphMetrics(glyphMetrics);
            }
            else
            {
                int missingGlyphWidth = (int)Math.Round(TextFormatterImp.RoundDip(emSize * GetAdvanceWidth(0, pixelsPerDip, textFormattingMode, isSideways), pixelsPerDip, textFormattingMode) * scalingFactor);
                for (int i = 0; i < stringLength; i++)
                {
                    advanceWidthsUnshaped[i] = missingGlyphWidth;
                }
            }
        }

        /// <summary>
        /// Compute an unshaped glyphrun object from specified character-based info
        /// </summary>
        internal GlyphRun ComputeUnshapedGlyphRun(
            Point origin,
            CharacterBufferRange charBufferRange,
            IList<double> charWidths,
            double emSize,
            float pixelsPerDip,
            double emHintingSize,
            bool nullGlyph,
            CultureInfo cultureInfo,
            string deviceFontName,
            TextFormattingMode textFormattingMode
            )
        {
            Debug.Assert(charBufferRange.Length > 0);

            CheckInitialized(); // This can only be called on fully initialized GlyphTypeface

            ushort[] nominalGlyphs = new ushort[charBufferRange.Length];

            // compute glyph positions

            if (nullGlyph)
            {
                for (int i = 0; i < charBufferRange.Length; i++)
                {
                    nominalGlyphs[i] = 0;
                }
            }
            else
            {
                GetGlyphIndicesOptimized(charBufferRange, nominalGlyphs, pixelsPerDip);
            }
            
            return GlyphRun.TryCreate(
                this,
                0,      // bidiLevel
                false,  // sideway
                emSize,
                pixelsPerDip,
                nominalGlyphs,
                origin,
                charWidths,
                null,   // glyphOffsets
                new PartialList<char>(charBufferRange.CharacterBuffer, charBufferRange.OffsetToFirstChar, charBufferRange.Length),
                deviceFontName,   // device font
                null,   // 1:1 mapping
                null,   // caret stops at every codepoint
                XmlLanguage.GetLanguage(cultureInfo.IetfLanguageTag),
                textFormattingMode
                );
        }

        /// <summary>
        /// GetGlyphIndicesOptimized will return the glyph indices in a ushort[] array
        /// It should not be used if both indices and advance widths are required. In that
        /// case use GetGlyphMetricsOptimized to get both.
        /// </summary>
        internal void GetGlyphIndicesOptimized(CharacterBufferRange characters, ushort[] glyphIndices, float pixelsPerDip)
        {
            // We don't need to pass real emSize, widths, TextFormattingMode and isSideways parameters, because 
            // they only matter for advance widths and we're only interested in getting glyph indices
            GetGlyphMetricsOptimized(characters, 0.0f, pixelsPerDip, glyphIndices, null, TextFormattingMode.Ideal, false);
        }

        /// <summary>
        /// Returns GlyphMetrics for a run of characters.  Heap allocation is typically avoided.
        /// </summary>
        internal void GetGlyphMetricsOptimized(CharacterBufferRange characters,
            double emSize,
            float pixelsPerDip,
            TextFormattingMode textFormattingMode,
            bool isSideways,
            MS.Internal.Text.TextInterface.GlyphMetrics[] glyphMetrics)
        {
            GetGlyphMetricsOptimized(characters, emSize, pixelsPerDip, null, glyphMetrics, textFormattingMode, isSideways);
        }

        /// <summary>
        /// Returns GlyphMetics and/or glyph indices matching a run of characters.  Heap allocation is typically avoided.
        /// </summary>     
        internal void GetGlyphMetricsOptimized(CharacterBufferRange characters,
            double emSize,
            float pixelsPerDip,
            ushort[] glyphIndices,
            MS.Internal.Text.TextInterface.GlyphMetrics[] glyphMetrics,
            TextFormattingMode textFormattingMode,
            bool isSideways)
        {
            Debug.Assert(glyphIndices != null || glyphMetrics != null);

            if (characters.Length * sizeof(uint) < GlyphRun.MaxStackAlloc)
            {
                unsafe
                {
                    uint *pCodepoints = stackalloc uint[characters.Length];
                    for (int i = 0; i < characters.Length; i++)
                    {
                        pCodepoints[i] = characters[i];
                    }
                    GetGlyphMetricsAndIndicesOptimized(pCodepoints, characters.Length, emSize, pixelsPerDip, glyphIndices, glyphMetrics, textFormattingMode, isSideways);
                }
            }
            else
            {
                uint[] codepoints = new uint[characters.Length];
                for (int i = 0; i < characters.Length; i++)
                {
                    codepoints[i] = characters[i];
                }
                unsafe
                {
                    fixed (uint *pCodepoints = &codepoints[0])
                    {
                        GetGlyphMetricsAndIndicesOptimized(pCodepoints, characters.Length, emSize, pixelsPerDip, glyphIndices, glyphMetrics, textFormattingMode, isSideways);
                    }
                }                
            }
        }

        private unsafe void GetGlyphMetricsAndIndicesOptimized(uint *pCodepoints, 
                                                               int characterCount, 
                                                               double emSize,
                                                               float pixelsPerDip,
                                                               ushort[] glyphIndices,
                                                               MS.Internal.Text.TextInterface.GlyphMetrics[] glyphMetrics,
                                                               TextFormattingMode textFormattingMode, 
                                                               bool isSideways)
        {
            bool releaseIndices = false;

            if (glyphIndices == null)
            {
                glyphIndices = BufferCache.GetUShorts(characterCount);
                releaseIndices = true;
            }

            fixed (ushort *pGlyphIndices = &glyphIndices[0])
            {
                _fontFace.CharacterMap.TryGetValues(pCodepoints, checked((uint)characterCount), pGlyphIndices);

                if (glyphMetrics != null)
                {
                    fixed (MS.Internal.Text.TextInterface.GlyphMetrics* pGlyphMetrics = &glyphMetrics[0])
                    {
                        GlyphMetrics(pGlyphIndices, characterCount, pGlyphMetrics, emSize, pixelsPerDip, textFormattingMode, isSideways);
                    }
                }
            }

            if (releaseIndices)
            {
                BufferCache.ReleaseUShorts(glyphIndices);
            }
        }            

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        internal FontSource FontSource
        {
            get
            {
                CheckInitialized(); // This can only be called on fully initialized GlyphTypeface
                return _fontSource;
            }
        }

        /// <summary>
        /// 0 for TTF files
        /// Face index within TrueType font collection for TTC files
        /// </summary>
        internal int FaceIndex
        {
            get
            {
                CheckInitialized(); // This can only be called on fully initialized GlyphTypeface
                int faceIndex;

                MS.Internal.Text.TextInterface.FontFace fontFaceDWrite = _font.GetFontFace();
                try
                {
                    faceIndex = checked((int)fontFaceDWrite.Index);
                }
                finally
                {
                    fontFaceDWrite.Release();
                }

                return faceIndex;
            }
        }

        internal FontFaceLayoutInfo FontFaceLayoutInfo
        {
            get
            {
                CheckInitialized(); // This can only be called on fully initialized GlyphTypeface
                return _fontFace;
            }
        }

        internal ushort BlankGlyphIndex
        {
            get
            {
                CheckInitialized(); // This can only be called on fully initialized GlyphTypeface
                return _fontFace.BlankGlyph;
            }
        }

        internal FontTechnology FontTechnology
        {
            get
            {
                CheckInitialized(); // This can only be called on fully initialized GlyphTypeface
                return _fontFace.FontTechnology;
            }
        }

        internal ushort DesignEmHeight
        {
            get
            {
                CheckInitialized(); // This can only be called on fully initialized GlyphTypeface
                return _font.Metrics.DesignUnitsPerEm;
            }
        }

        //
        // Need ability to add ref and get pointer to the DWrite font face for the rendering
        // thread to access
        //
        unsafe internal IntPtr GetDWriteFontAddRef
        {
            get
            {
                CheckInitialized();
                return _font.DWriteFontAddRef;
            }
        }
        
        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        private IDictionary<CultureInfo, string> GetFontInfo(MS.Internal.Text.TextInterface.InformationalStringID informationalStringID)
        {
            MS.Internal.Text.TextInterface.LocalizedStrings localizedStrings;
            if (_font.GetInformationalStrings(informationalStringID, out localizedStrings))
            {
                return localizedStrings;
            }
            else
            {
                return new MS.Internal.Text.TextInterface.LocalizedStrings();
            }
        }
 
        #endregion Private Methods

        #region ISupportInitialize interface

        void ISupportInitialize.BeginInit()
        {
            if (_initializationState == InitializationState.IsInitialized)
            {
                // Cannot initialize a GlyphRun this is completely initialized.
                throw new InvalidOperationException(SR.Get(SRID.OnlyOneInitialization));
            }

            if (_initializationState == InitializationState.IsInitializing)
            {
                // Cannot initialize a GlyphRun this already being initialized.
                throw new InvalidOperationException(SR.Get(SRID.InInitialization));
            }

            _initializationState = InitializationState.IsInitializing;
        }

        void ISupportInitialize.EndInit()
        {
            if (_initializationState != InitializationState.IsInitializing)
            {
                // Cannot EndInit a GlyphRun that is not being initialized.
                throw new InvalidOperationException(SR.Get(SRID.NotInInitialization));
            }

            Initialize(
                (_originalUri == null) ? null : _originalUri.Value,
                 _styleSimulations
                 );
        }

        private void CheckInitialized()
        {
            if (_initializationState != InitializationState.IsInitialized)
            {
                throw new InvalidOperationException(SR.Get(SRID.InitializationIncomplete));
            }
        }

        private void CheckInitializing()
        {
            if (_initializationState != InitializationState.IsInitializing)
            {
                throw new InvalidOperationException(SR.Get(SRID.NotInInitialization));
            }
        }

        /// <summary>
        /// Allocates a GlyphIndexer for the specified accessor.
        /// </summary>
        private GlyphIndexer CreateGlyphIndexer(GlyphAccessor accessor)
        {
            GlyphIndexer indexer;

            MS.Internal.Text.TextInterface.FontFace fontFaceDWrite = _font.GetFontFace();
            try
            {
                indexer = new GlyphIndexer(accessor, fontFaceDWrite.GlyphCount);
            }
            finally
            {
                fontFaceDWrite.Release();
            }

            return indexer;
        }

        #endregion

        //------------------------------------------------------
        //
        //  Private Nested Classes
        //
        //------------------------------------------------------

        #region Private Nested Classes

        private delegate double GlyphAccessor(ushort glyphIndex, float pixelsPerDip, TextFormattingMode textFormattingMode, bool isSideways);

        /// <summary>
        /// This class is a helper to implement named indexers
        /// for glyph metrics.
        /// </summary>
        private class GlyphIndexer : IDictionary<ushort, double>
        {
            internal GlyphIndexer(GlyphAccessor accessor, ushort numberOfGlyphs)
            {
                _accessor = accessor;
                _numberOfGlyphs = numberOfGlyphs;
            }

            #region IDictionary<ushort,double> Members

            public void Add(ushort key, double value)
            {
                throw new NotSupportedException();
            }

            public bool ContainsKey(ushort key)
            {
                return (key < _numberOfGlyphs);
            }

            public ICollection<ushort> Keys
            {
                get { return new SequentialUshortCollection(_numberOfGlyphs); }
            }

            public bool Remove(ushort key)
            {
                throw new NotSupportedException();
            }

            public bool TryGetValue(ushort key, out double value)
            {
                if (ContainsKey(key))
                {
                    value = this[key];
                    return true;
                }
                else
                {
                    value = new double();
                    return false;
                }
            }

            public ICollection<double> Values
            {
                get { return new ValueCollection(this); }
            }

            public double this[ushort key]
            {
                get
                {
                    // ?????? hardcoded 1.0 as pixelsPerDip, as this is for Ideal Mode, and therefore will not be needed.
                    // Discuss implications.
                    return _accessor(key, (float)1.0, TextFormattingMode.Ideal, false);
                }
                set
                {
                    throw new NotSupportedException();
                }
            }

            #endregion

            #region ICollection<KeyValuePair<ushort,double>> Members

            public void Add(KeyValuePair<ushort, double> item)
            {
                throw new NotSupportedException();
            }

            public void Clear()
            {
                throw new NotSupportedException();
            }

            public bool Contains(KeyValuePair<ushort, double> item)
            {
                return ContainsKey(item.Key);
            }

            public void CopyTo(KeyValuePair<ushort, double>[] array, int arrayIndex)
            {
                if (array == null)
                {
                    throw new ArgumentNullException("array");
                }

                if (array.Rank != 1)
                {
                    throw new ArgumentException(SR.Get(SRID.Collection_BadRank));
                }

                // The extra "arrayIndex >= array.Length" check in because even if _collection.Count
                // is 0 the index is not allowed to be equal or greater than the length
                // (from the MSDN ICollection docs)
                if (arrayIndex < 0 || arrayIndex >= array.Length || (arrayIndex + Count) > array.Length)
                {
                    throw new ArgumentOutOfRangeException("arrayIndex");
                }

                for (ushort i = 0; i < Count; ++i)
                    array[arrayIndex + i] = new KeyValuePair<ushort, double>(i, this[i]);
            }

            public int Count
            {
                get { return _numberOfGlyphs; }
            }

            public bool IsReadOnly
            {
                get { return true; }
            }

            public bool Remove(KeyValuePair<ushort, double> item)
            {
                throw new NotSupportedException();
            }

            #endregion

            #region IEnumerable<KeyValuePair<ushort,double>> Members

            public IEnumerator<KeyValuePair<ushort, double>> GetEnumerator()
            {
                for (ushort i = 0; i < Count; ++i)
                    yield return new KeyValuePair<ushort, double>(i, this[i]);
            }

            #endregion

            #region IEnumerable Members

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable<KeyValuePair<ushort, double>>)this).GetEnumerator();
            }

            #endregion

            private class ValueCollection : ICollection<double>
            {
                public ValueCollection(GlyphIndexer glyphIndexer)
                {
                    _glyphIndexer = glyphIndexer;
                }

                #region ICollection<double> Members

                public void Add(double item)
                {
                    throw new NotSupportedException();
                }

                public void Clear()
                {
                    throw new NotSupportedException();
                }

                public bool Contains(double item)
                {
                    foreach (double d in this)
                    {
                        if (d == item)
                            return true;
                    }
                    return false;
                }

                public void CopyTo(double[] array, int arrayIndex)
                {
                    if (array == null)
                    {
                        throw new ArgumentNullException("array");
                    }

                    if (array.Rank != 1)
                    {
                        throw new ArgumentException(SR.Get(SRID.Collection_BadRank));
                    }

                    // The extra "arrayIndex >= array.Length" check in because even if _collection.Count
                    // is 0 the index is not allowed to be equal or greater than the length
                    // (from the MSDN ICollection docs)
                    if (arrayIndex < 0 || arrayIndex >= array.Length || (arrayIndex + Count) > array.Length)
                    {
                        throw new ArgumentOutOfRangeException("arrayIndex");
                    }

                    for (ushort i = 0; i < Count; ++i)
                        array[arrayIndex + i] = _glyphIndexer[i];
                }

                public int Count
                {
                    get { return _glyphIndexer._numberOfGlyphs; }
                }

                public bool IsReadOnly
                {
                    get { return true; }
                }

                public bool Remove(double item)
                {
                    throw new NotSupportedException();
                }

                #endregion

                #region IEnumerable<double> Members

                public IEnumerator<double> GetEnumerator()
                {
                    for (ushort i = 0; i < Count; ++i)
                        yield return _glyphIndexer[i];
                }

                #endregion

                #region IEnumerable Members

                IEnumerator IEnumerable.GetEnumerator()
                {
                    return ((IEnumerable<double>)this).GetEnumerator();
                }

                #endregion

                private GlyphIndexer _glyphIndexer;
            }

            private GlyphAccessor _accessor;
            private ushort _numberOfGlyphs;
        }

        #endregion Private Nested Classes

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private FontFaceLayoutInfo          _fontFace;

        private StyleSimulations            _styleSimulations;

        private MS.Internal.Text.TextInterface.Font     _font;

        private FontSource                  _fontSource;


        /// <summary>
        /// The Uri that was passed in to constructor.
        /// </summary>
        private SecurityCriticalDataClass<Uri> _originalUri;

        private const double CFFConversionFactor = 1.0 / 65536.0;

        private InitializationState _initializationState;

        /// <summary>
        /// Initialization states of GlyphTypeface object.
        /// </summary>
        private enum InitializationState
        {
            /// <summary>
            /// The state in which the GlyphTypeface has not been initialized.
            /// At this state, all operations on the object would cause InvalidOperationException.
            /// The object can only transit to 'IsInitializing' state with BeginInit() call.
            /// </summary>
            Uninitialized,

            /// <summary>
            /// The state in which the GlyphTypeface is being initialized. At this state, user can
            /// set values into the required properties. The object can only transit to 'IsInitialized' state
            /// with EndInit() call.
            /// </summary>
            IsInitializing,

            /// <summary>
            /// The state in which the GlyphTypeface object is fully initialized. At this state the object
            /// is fully functional. There is no valid transition out of the state.
            /// </summary>
            IsInitialized,
        }

        #endregion Private Fields
    }
}
