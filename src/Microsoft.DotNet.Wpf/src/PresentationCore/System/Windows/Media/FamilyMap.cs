// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//+-----------------------------------------------------------------------
//
//
//
//  Contents:  FontFamilyMap implementation
//
//  Spec:      Fonts.htm
//
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security;
using System.Globalization;
using System.ComponentModel;
using System.Windows.Markup;
using MS.Internal.FontFace;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace System.Windows.Media
{
    /// <summary>
    /// Defines which FontFamily to use for a specified set of Unicode code points and
    /// a specified language. The FontFamilyMap also specifies a scale factor, allowing the
    /// target FontFamily size to be adjusted to better match the size of other fonts
    /// used in the composite font family.
    /// </summary>
    public class FontFamilyMap
    {
        private Range[]     _ranges;
        private XmlLanguage _language;
        private double      _scaleInEm;
        private string      _targetFamilyName;

        internal const int LastUnicodeScalar = 0x10ffff;
        private static readonly Range[] _defaultRanges = new Range[] { new Range(0, LastUnicodeScalar) };

        internal static readonly FontFamilyMap Default = new FontFamilyMap(
            0, 
            LastUnicodeScalar,
            null,   // any language
            null,   // Target
            1.0     // Scale
            );

        /// <summary>
        /// Construct a default family map object
        /// </summary>
        public FontFamilyMap() 
            : this(
                0,
                LastUnicodeScalar,
                null,   // any language
                null,   // Target
                1.0     // Scale
                )
        {}
        

        /// <summary>
        /// Construct a Family map object
        /// </summary>
        /// <param name="firstChar">first character</param>
        /// <param name="lastChar">last character</param>
        /// <param name="language">language</param>
        /// <param name="targetFamilyName">target family name</param>
        /// <param name="scaleInEm">font scale in EM</param>
        internal FontFamilyMap(
            int             firstChar,
            int             lastChar,
            XmlLanguage     language,
            string          targetFamilyName,
            double          scaleInEm
            )
        {
            if (firstChar == 0 && lastChar == LastUnicodeScalar)
                _ranges = _defaultRanges;
            else
                _ranges = new Range[]{ new Range(firstChar, lastChar) };

            _language = language;
            _scaleInEm = scaleInEm;
            _targetFamilyName = targetFamilyName;
        }


        /// <summary>
        /// String of Unicode ranges as a list of 'FirstCode-LastCode' separated by comma
        /// e.g. "0000-00ff,00e0-00ef"
        /// </summary>
        [DesignerSerializationOptions(DesignerSerializationOptions.SerializeAsAttribute)]
        public string Unicode
        {
            set 
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                _ranges = ParseUnicodeRanges(value); 
            }

            get 
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();

                for (int i = 0; i < _ranges.Length; ++i)
                {
                    if (i != 0) sb.Append(',');
                    sb.AppendFormat(NumberFormatInfo.InvariantInfo, "{0:x4}-{1:x4}", _ranges[i].First, _ranges[i].Last);
                }

                return sb.ToString();
            }
        }

        /// <summary>
        /// Target font family name in which the ranges map to
        /// </summary>
        [DesignerSerializationOptions(DesignerSerializationOptions.SerializeAsAttribute)]
        public string Target
        {
            get 
            { 
                return _targetFamilyName; 
            }

            set 
            {
                _targetFamilyName = value;
            }
        }


        /// <summary>
        /// Font scaling factor relative to EM
        /// </summary>
        public double Scale
        {
            get 
            { 
                return _scaleInEm; 
            }

            set
            {
                CompositeFontParser.VerifyPositiveMultiplierOfEm("Scale", ref value);
                _scaleInEm = value;
            }
        }


        /// <summary>
        /// Language to which the FontFamilyMap applies.
        /// </summary>
        /// <remarks>
        /// This property can be a specific language if the FontFamilyMap applies to just that 
        /// language, a neutral language if it applies to a group of related languages, or 
        /// the empty string if it applies to any language. The default value is the empty string.
        /// </remarks>
        public XmlLanguage Language
        {
            get 
            { 
                return _language; 
            }

            set 
            {
                _language = (value == XmlLanguage.Empty) ? null : value;
                _language = value;
            }
        }


        /// <summary>
        /// Indicates whether the FontFamilyMap is a simple one such as produced
        /// by common cases like "Tahoma,Verdana".
        /// </summary>
        /// <remarks>
        /// A simple family map matches all code points for all languages 
        /// with no scaling. In other words, all properties except Target 
        /// have default values.
        /// </remarks>
        internal bool IsSimpleFamilyMap
        {
            get
            {
                return _language == null &&
                    _scaleInEm == 1.0 &&
                    _ranges == _defaultRanges;
            }
        }


        internal static bool MatchLanguage(XmlLanguage familyMapLanguage, XmlLanguage language)
        {
            // If there is no family map langue, the family map applies to any language.
            if (familyMapLanguage == null)
            {
                return true;
            }

            if (language != null)
            {
                return familyMapLanguage.RangeIncludes(language);
            }   

            return false;
        }
        
        internal static bool MatchCulture(XmlLanguage familyMapLanguage, CultureInfo culture)
        {
            // If there is no family map langue, the family map applies to any language.
            if (familyMapLanguage == null)
            {
                return true;
            }

            if (culture != null)
            {
                return familyMapLanguage.RangeIncludes(culture);
            }   

            return false;
        }

        internal Range[] Ranges
        {
            get { return _ranges; }
        }

        private static void ThrowInvalidUnicodeRange()
        {
            throw new FormatException(SR.Get(SRID.CompositeFontInvalidUnicodeRange));
        }

        private static Range[] ParseUnicodeRanges(string unicodeRanges)
        {
            List<Range> ranges = new List<Range>(3);
            int index = 0;
            while (index < unicodeRanges.Length)
            {
                int firstNum;
                if (!ParseHexNumber(unicodeRanges, ref index, out firstNum))
                {
                    ThrowInvalidUnicodeRange();
                }

                int lastNum = firstNum;

                if (index < unicodeRanges.Length)
                {
                    if (unicodeRanges[index] == '?')
                    {
                        do
                        {
                            firstNum = firstNum * 16;
                            lastNum = lastNum * 16 + 0x0F;
                            index++;
                        } while (
                            index < unicodeRanges.Length && 
                            unicodeRanges[index] == '?' &&
                            lastNum <= LastUnicodeScalar);
                    }
                    else if (unicodeRanges[index] == '-')
                    {
                        index++; // pass '-' character
                        if (!ParseHexNumber(unicodeRanges, ref index, out lastNum))
                        {
                            ThrowInvalidUnicodeRange();
                        }
                    }
                }

                if (firstNum > lastNum ||
                    lastNum > LastUnicodeScalar ||
                    (index<unicodeRanges.Length && unicodeRanges[index] !=','))
                {
                    ThrowInvalidUnicodeRange();
                }

                ranges.Add(new Range(firstNum, lastNum));

                index++; // ranges seperator comma
            }

            return ranges.ToArray();
        }

        /// <summary>
        /// helper method to convert a string (written as hex number) into number.
        /// </summary>
        internal static bool ParseHexNumber(string numString, ref int index, out int number)
        {
            while (index<numString.Length && numString[index] == ' ') 
            {
                index++;
            }
            
            int startIndex = index;

            number = 0;

            while (index < numString.Length) 
            {
                int n = (int) numString[index];
                if (n >= (int)'0' && n <= (int)'9')
                {
                    number = (number * 0x10) + (n - ((int)'0'));
                    index++;
                }
                else
                {
                    n |= 0x20; // [A-F] --> [a-f]
                    if (n >= (int)'a' && n <= (int)'f')
                    {
                        number = (number * 0x10) + (n - ((int)'a' - 10));
                        index++;
                    }
                    else
                    {
                        break;
                    }
                }
            } 

            bool retValue = index > startIndex;
            
            while (index < numString.Length && numString[index] == ' ') 
            {
                index++;
            }
            
            return retValue;
        }


        internal bool InRange(int ch)
        {
            for(int i = 0; i < _ranges.Length; i++)
            {
                Range r = _ranges[i];
                if(r.InRange(ch))
                    return true;
            }
            return false;
        }


        /// <summary>
        /// Unicode range
        /// </summary>
        internal class Range
        {
            private int     _first;
            private uint    _delta;

            internal Range(
                int     first,
                int     last
                )
            {
                Debug.Assert(first <= last);
                _first = first;
                _delta = (uint)(last - _first); // used in range testing
            }

            internal bool InRange(int ch)
            {
                // clever code from Word meaning: "ch >= _first && ch <= _last",
                // this is done with one test and branch.
                return (uint)(ch - _first) <= _delta;
            }

            internal int First
            {
                get { return _first; }
            }
            
            internal int Last
            {
                get { return _first + (int) _delta; }
            }
            
            internal uint Delta
            {
                get { return _delta; }
            }
        }
    }
}
