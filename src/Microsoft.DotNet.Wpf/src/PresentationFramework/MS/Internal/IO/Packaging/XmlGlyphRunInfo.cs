// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//              Implements a DOM-based subclass of the FixedPageInfo abstract class.
//              The class functions as an array of XmlGlyphRunInfo's in markup order.
//

using System;
using System.Xml;                       // For DOM objects
using System.Diagnostics;               // For Assert
using System.Globalization;             // For CultureInfo
using System.Windows;                   // For ExceptionStringTable
using Windows = System.Windows;         // For Windows.Point (as distinct from System.Drawing.Point)
using System.Windows.Markup;            // For XmlLanguage

namespace MS.Internal.IO.Packaging
{
    /// <summary>
    /// DOM-based implementation of the abstract class GlyphRunInfo.
    /// </summary>
    internal class XmlGlyphRunInfo : MS.Internal.GlyphRunInfo
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors
        /// <summary>
        /// Initialize from a DOM object.
        /// </summary>
        internal XmlGlyphRunInfo(XmlNode glyphsNode)
        {
            _glyphsNode = glyphsNode as XmlElement;
            // Assert that XmlFixedPageInfo (only caller) has correctly identified glyph runs
            // prior to invoking this constructor.
            Debug.Assert(_glyphsNode != null 
                && String.CompareOrdinal(_glyphsNode.LocalName, _glyphRunName) == 0
                && String.CompareOrdinal(_glyphsNode.NamespaceURI, ElementTableKey.FixedMarkupNamespace) == 0);
        }
        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties
        /// <summary>
        /// The start point of the segment [StartPosition, EndPosition],
        /// which runs along the baseline of the glyph run.
        /// </summary>
        /// <remarks>
        /// The point is given in page coordinates.
        /// double.NaN can be returned in either coordinate when the input glyph run is invalid.
        /// </remarks>
        internal override Windows.Point StartPosition 
        { 
            get
            {
                throw new NotSupportedException(SR.Get(SRID.XmlGlyphRunInfoIsNonGraphic));
            }
        }

        /// <summary>
        /// The end point of the segment [StartPosition, EndPosition],
        /// which runs along the baseline of the glyph run.
        /// </summary>
        /// <remarks>
        /// The point is given in page coordinates.
        /// double.NaN can be returned in either coordinate when the input glyph run is invalid.
        /// </remarks>
        internal override Windows.Point EndPosition 
        { 
            get
            {
                throw new NotSupportedException(SR.Get(SRID.XmlGlyphRunInfoIsNonGraphic));
            }
        }


        /// <summary>
        /// The font width in ems.
        /// </summary>
        /// <remarks>
        /// This is provided for the purpose of evaluating distances along the baseline OR a perpendicular
        /// to the baseline.
        /// When a font is displayed sideways, what is given here is still the width of the font.
        /// It is up to the client code to decide whether to use the width or height for measuring
        /// distances between glyph runs.
        ///
        /// NaN can be returned if the markup is invalid.
        /// </remarks>
        internal override double WidthEmFontSize 
        { 
            get
            {
                throw new NotSupportedException(SR.Get(SRID.XmlGlyphRunInfoIsNonGraphic));
            }
        }

        /// <summary>
        /// The font height in ems.
        /// </summary>
        /// <remarks>
        /// This is provided for the purpose of evaluating distances along the baseline OR a perpendicular
        /// to the baseline.
        /// When a font is displayed sideways, what is given here is still the height of the font.
        /// It is up to the client code to decide whether to use the width or height for measuring
        /// deviations from the current baseline.
        ///
        /// NaN can be returned if the markup is invalid.
        /// </remarks>
        internal override double HeightEmFontSize 
        { 
            get
            {
                throw new NotSupportedException(SR.Get(SRID.XmlGlyphRunInfoIsNonGraphic));
            }
        }

        /// <summary>
        /// Whether glyphs are individually rotated 90 degrees (so as to face downwards in vertical text layout).
        /// </summary>
        /// <remarks>
        /// This feature is designed for ideograms and should not make sense for latin characters.
        /// </remarks>
        internal override bool GlyphsHaveSidewaysOrientation 
        { 
            get
            {
                throw new NotSupportedException(SR.Get(SRID.XmlGlyphRunInfoIsNonGraphic));
            }
        }

        /// <summary>
        /// 0 for left-to-right and 1 for right-to-left.
        /// </summary>
        /// <remarks>
        /// 0 is assumed if the attribute value is absent or unexpected.
        /// </remarks>
        internal override int BidiLevel 
        { 
            get
            {
                throw new NotSupportedException(SR.Get(SRID.XmlGlyphRunInfoIsNonGraphic));
            }
        }

        /// <summary>
        /// The glyph run's language id.
        /// </summary>
        /// <remarks>
        /// The language ID is typed as unsigned for easier interop marshalling, since the win32 LCID type (as defined in WinNT.h) is a DWORD.
        /// </remarks>
        internal override uint LanguageID
        {
            get
            {
                if (_languageID == null)
                {
                    for (XmlElement currentNode = _glyphsNode; 
                         currentNode != null && _languageID == null; 
                         currentNode = (currentNode.ParentNode as XmlElement))
                    {
                        string languageString = currentNode.GetAttribute(_xmlLangAttribute);
                        if (languageString != null && languageString.Length > 0)
                        {
                            // We need to handle languageString "und" specially. 
                            // we should set language ID to zero. 
                            // That's what the Indexing Search team told us to do.
                            // There's no CultureInfo for "und". 
                            // CultureInfo("und") will cause an error.
                            if (string.CompareOrdinal(languageString.ToUpperInvariant(), _undeterminedLanguageStringUpper) == 0)
                            {
                                _languageID = 0;
                            }
                            else
                            {
                                // Here we use XmlLanguage class to help us get the most 
                                // compatible culture from the languageString.
                                //
                                // In the case that languageString and its variants do not match 
                                // any known language string in the table, we will get InvariantCulture
                                // from GetCompatibleCulture().
                                // 
                                // In the case that languageString is invalid (e.g. non-ascii string),
                                // the GetLanguage() method will throw an exception and we will give 
                                // up filtering this part.
                                XmlLanguage lang = XmlLanguage.GetLanguage(languageString);
                                CultureInfo cultureInfo = lang.GetCompatibleCulture();
                                _languageID = checked((uint)cultureInfo.LCID);
                            }
                        }
                    }

                    // If we cannot set the language ID in the previous logic, that means the 
                    // language string is missing and we should default the culture to be 
                    // InvariantCulture.
                    // Note: XamlFilter.GetCurrentLcid is a private method that also has
                    // similar logic and will default to CultureInfo.InvariantCulture.LCID
                    // CultureInfo.InvariantCulture will never be null
                    if(_languageID == null)
                        _languageID = checked((uint)CultureInfo.InvariantCulture.LCID); 
                }
                // Cast Nullable<> into value type.
                return (uint) _languageID;
            }
        }
        
        /// <summary>
        /// The glyph run's contents as a string of unicode symbols.
        /// If the unicode attribute is missing in the markup then an empty string is returned.
        /// </summary>        
        internal override string UnicodeString 
        { 
            get
            {
                if (_unicodeString == null)
                {
                    _unicodeString = _glyphsNode.GetAttribute(_unicodeStringAttribute);
                }
                return _unicodeString;
            }
        }
        #endregion Internal Properties


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields

        #region Constants
        private const string _glyphRunName           = "Glyphs";
        private const string _xmlLangAttribute       = "xml:lang";
        private const string _unicodeStringAttribute = "UnicodeString";

        // The undetermined language string can be "und" or "UND". We always convert strings
        // to uppercase for case-insensitive comparison, so we store an uppercase version here.
        private const string _undeterminedLanguageStringUpper = "UND";

        private XmlElement _glyphsNode = null;
        private string _unicodeString = null;
        private Nullable<uint> _languageID = null;

        #endregion Private Fields
    }

    #endregion NativeMethods
}
