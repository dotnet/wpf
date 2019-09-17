// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//+-----------------------------------------------------------------------
//
//
//
//  Contents:  FamilyTypeface implementation
//
//  Spec:      Fonts.htm
//
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.ComponentModel;
using System.Windows.Markup;    // for XmlLanguage
using MS.Internal.FontFace;
using System.Security;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media
{
    /// <summary>
    /// The FamilyTypeface object specifies the details of a single typeface supported by a
    /// FontFamily. There are as many FamilyTypeface objects as there are typefaces supported.
    /// </summary>
    public class FamilyTypeface : IDeviceFont, ITypefaceMetrics
    {
        /// <summary>
        /// Construct a default family typeface
        /// </summary>
        public FamilyTypeface()
        {}


        /// <summary>
        /// Construct a read-only FamilyTypeface from a Typeface.
        /// </summary>
        internal FamilyTypeface(Typeface face)
        {
            _style = face.Style;
            _weight = face.Weight;
            _stretch = face.Stretch;
            _underlinePosition = face.UnderlinePosition;
            _underlineThickness = face.UnderlineThickness;
            _strikeThroughPosition = face.StrikethroughPosition;
            _strikeThroughThickness = face.StrikethroughThickness;
            _capsHeight = face.CapsHeight;
            _xHeight = face.XHeight;
            _readOnly = true;
        }


        /// <summary>
        /// Typeface style
        /// </summary>
        public FontStyle Style
        {
            get { return _style;  }
            set
            {
                VerifyChangeable();
                _style = value;
            }
        }


        /// <summary>
        /// Typeface weight
        /// </summary>
        public FontWeight Weight
        {
            get { return _weight; }
            set
            {
                VerifyChangeable();
                _weight = value;
            }
        }


        /// <summary>
        /// Typeface stretch
        /// </summary>
        public FontStretch Stretch
        {
            get { return _stretch;  }
            set
            {
                VerifyChangeable();
                _stretch = value;
            }
        }


        /// <summary>
        /// Typeface underline position in EM relative to baseline
        /// </summary>
        public double UnderlinePosition
        {
            get { return _underlinePosition ; }
            set
            {
                CompositeFontParser.VerifyMultiplierOfEm("UnderlinePosition", ref value);
                VerifyChangeable();
                _underlinePosition = value;
            }
        }


        /// <summary>
        /// Typeface underline thickness in EM
        /// </summary>
        public  double UnderlineThickness
        {
            get { return _underlineThickness; }
            set
            {
                CompositeFontParser.VerifyPositiveMultiplierOfEm("UnderlineThickness", ref value);
                VerifyChangeable();
                _underlineThickness = value;
            }
        }


        /// <summary>
        /// Typeface strikethrough position in EM relative to baseline
        /// </summary>
        public double StrikethroughPosition
        {
            get { return _strikeThroughPosition;  }
            set
            {
                CompositeFontParser.VerifyMultiplierOfEm("StrikethroughPosition", ref value);
                VerifyChangeable();
                _strikeThroughPosition = value;
            }
        }


        /// <summary>
        /// Typeface strikethrough thickness in EM
        /// </summary>
        public double StrikethroughThickness
        {
            get { return _strikeThroughThickness;  }
            set
            {
                CompositeFontParser.VerifyPositiveMultiplierOfEm("StrikethroughThickness", ref value);
                VerifyChangeable();
                _strikeThroughThickness = value;
            }
        }


        /// <summary>
        /// Typeface caps height in EM
        /// </summary>
        public double CapsHeight
        {
            get { return _capsHeight;  }
            set
            {
                CompositeFontParser.VerifyPositiveMultiplierOfEm("CapsHeight", ref value);
                VerifyChangeable();
                _capsHeight = value;
            }
        }

        /// <summary>
        /// Typeface X-height in EM
        /// </summary>
        public double XHeight
        {
            get { return _xHeight;  }
            set
            {
                CompositeFontParser.VerifyPositiveMultiplierOfEm("XHeight", ref value);
                VerifyChangeable();
                _xHeight = value;
            }
        }

        /// <summary>
        /// Flag indicate whether this is symbol typeface; always false for FamilyTypeface.
        /// </summary>
        bool ITypefaceMetrics.Symbol
        {
            get { return false; }
        }

        /// <summary>
        /// Style simulation flags for this typeface.
        /// </summary>
        StyleSimulations ITypefaceMetrics.StyleSimulations
        {
            get
            {
                return StyleSimulations.None;
            }
        }

        /// <summary>
        /// Collection of localized face names adjusted by the font differentiator.
        /// </summary>
        public IDictionary<XmlLanguage, string> AdjustedFaceNames
        {
            get
            {
                return FontDifferentiator.ConstructFaceNamesByStyleWeightStretch(_style, _weight, _stretch);
            }
        }

        /// <summary>
        /// Compares two typefaces for equality; returns true if the specified typeface
        /// is not null and has the same properties as this typeface.
        /// </summary>
        public bool Equals(FamilyTypeface typeface)
        {
            if (typeface == null)
                return false;

            return (
                 Style   == typeface.Style
              && Weight  == typeface.Weight
              && Stretch == typeface.Stretch
              );
        }


        /// <summary>
        /// Name or unique identifier of a device font.
        /// </summary>
        public string DeviceFontName
        {
            get { return _deviceFontName; }
            set
            {
                VerifyChangeable();
                _deviceFontName = value;
            }
        }


        /// <summary>
        /// Collection of character metrics for a device font.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public CharacterMetricsDictionary DeviceFontCharacterMetrics
        {
            get
            {
                if (_characterMetrics == null)
                {
                    _characterMetrics = new CharacterMetricsDictionary();
                }
                return _characterMetrics;
            }
        }


        /// <summary>
        /// <see cref="object.Equals(object)"/>
        /// </summary>
        public override bool Equals(object o)
        {
            return Equals(o as FamilyTypeface);
        }

        /// <summary>
        /// <see cref="object.GetHashCode"/>
        /// </summary>
        public override int GetHashCode()
        {
            return  _style.GetHashCode()
                  ^ _weight.GetHashCode()
                  ^ _stretch.GetHashCode();
        }

        private void VerifyChangeable()
        {
            if (_readOnly)
                throw new NotSupportedException(SR.Get(SRID.General_ObjectIsReadOnly));
        }

        string IDeviceFont.Name
        {
            get { return _deviceFontName; }
        }

        bool IDeviceFont.ContainsCharacter(int unicodeScalar)
        {
            return _characterMetrics != null && _characterMetrics.GetValue(unicodeScalar) != null;
        }


        unsafe void IDeviceFont.GetAdvanceWidths(
            char*   characterString,
            int     characterLength,
            double  emSize,
            int*    pAdvances
        )
        {
            unsafe
            {
                for (int i = 0; i < characterLength; ++i)
                {
                    CharacterMetrics metrics = _characterMetrics.GetValue(characterString[i]);
                    if (metrics != null)
                    {
                        // Side bearings are included in the advance width but are not used as offsets for glyph positioning.
                        pAdvances[i] = Math.Max(0, (int)((metrics.BlackBoxWidth + metrics.LeftSideBearing + metrics.RightSideBearing) * emSize));
                    }
                    else
                    {
                        pAdvances[i] = 0;
                    }
                }
            }
        }




        private bool            _readOnly;
        private FontStyle       _style;
        private FontWeight      _weight;
        private FontStretch     _stretch;
        private double          _underlinePosition;
        private double          _underlineThickness;
        private double          _strikeThroughPosition;
        private double          _strikeThroughThickness;
        private double          _capsHeight;
        private double          _xHeight;
        private string          _deviceFontName;
        private CharacterMetricsDictionary _characterMetrics;
    }
}
