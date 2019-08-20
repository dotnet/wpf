// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//  Synopsis: Implements class Parsers for internal use of type converters
//

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using MS.Internal;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace MS.Internal
{
    internal static partial class Parsers
    {
        private const int s_zeroChar = (int) '0';
        private const int s_aLower   = (int) 'a';
        private const int s_aUpper   = (int) 'A';

        static private int ParseHexChar(char c )
        {
            int intChar = (int) c;

            if ((intChar >= s_zeroChar) && (intChar <= (s_zeroChar+9)))
            {
                return (intChar-s_zeroChar);
            }

            if ((intChar >= s_aLower) && (intChar <= (s_aLower+5)))
            {
                return (intChar-s_aLower + 10);
            }

            if ((intChar >= s_aUpper) && (intChar <= (s_aUpper+5)))
            {
                return (intChar-s_aUpper + 10);
            }
            throw new FormatException(SR.Get(SRID.Parsers_IllegalToken));
        }

        static private Color ParseHexColor(string trimmedColor)
        {
            int a,r,g,b;
            a = 255;

            if ( trimmedColor.Length > 7 )
            {
                a = ParseHexChar(trimmedColor[1]) * 16 + ParseHexChar(trimmedColor[2]);
                r = ParseHexChar(trimmedColor[3]) * 16 + ParseHexChar(trimmedColor[4]);
                g = ParseHexChar(trimmedColor[5]) * 16 + ParseHexChar(trimmedColor[6]);
                b = ParseHexChar(trimmedColor[7]) * 16 + ParseHexChar(trimmedColor[8]);
            }
            else if ( trimmedColor.Length > 5)
            {
                r = ParseHexChar(trimmedColor[1]) * 16 + ParseHexChar(trimmedColor[2]);
                g = ParseHexChar(trimmedColor[3]) * 16 + ParseHexChar(trimmedColor[4]);
                b = ParseHexChar(trimmedColor[5]) * 16 + ParseHexChar(trimmedColor[6]);
            }
            else if (trimmedColor.Length > 4)
            {
                a = ParseHexChar(trimmedColor[1]);
                a = a + a*16;
                r = ParseHexChar(trimmedColor[2]);
                r = r + r*16;
                g = ParseHexChar(trimmedColor[3]);
                g = g + g*16;
                b = ParseHexChar(trimmedColor[4]);
                b = b + b*16;
            }
            else
            {
                r = ParseHexChar(trimmedColor[1]);
                r = r + r*16;
                g = ParseHexChar(trimmedColor[2]);
                g = g + g*16;
                b = ParseHexChar(trimmedColor[3]);
                b = b + b*16;
            }

            return ( Color.FromArgb ((byte)a, (byte)r, (byte)g, (byte)b) );
        }

    internal const string s_ContextColor = "ContextColor ";
    internal const string s_ContextColorNoSpace = "ContextColor";

    static private Color ParseContextColor(string trimmedColor, IFormatProvider formatProvider, ITypeDescriptorContext context)
        {
            if (!trimmedColor.StartsWith(s_ContextColor, StringComparison.OrdinalIgnoreCase))
            {
                throw new FormatException(SR.Get(SRID.Parsers_IllegalToken));
            }

            string tokens = trimmedColor.Substring(s_ContextColor.Length);
            tokens = tokens.Trim();
            string[] preSplit = tokens.Split(new Char[] { ' ' });
            if (preSplit.GetLength(0)< 2)
            {
                throw new FormatException(SR.Get(SRID.Parsers_IllegalToken));
            }

            tokens = tokens.Substring(preSplit[0].Length);

            TokenizerHelper th = new TokenizerHelper(tokens, formatProvider);
            string[] split = tokens.Split(new Char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            int numTokens = split.GetLength(0);

            float alpha = Convert.ToSingle(th.NextTokenRequired(), formatProvider);

            float[] values = new float[numTokens - 1];

            for (int i = 0; i < numTokens - 1; i++)
            {
                values[i] = Convert.ToSingle(th.NextTokenRequired(), formatProvider);
            }

            string profileString = preSplit[0];

            UriHolder uriHolder = TypeConverterHelper.GetUriFromUriContext(context,profileString);

            Uri profileUri;

            if (uriHolder.BaseUri != null)
            {
                profileUri = new Uri(uriHolder.BaseUri, uriHolder.OriginalUri);
            }
            else
            {
                profileUri = uriHolder.OriginalUri;
            }

            Color result = Color.FromAValues(alpha, values, profileUri);

            // If the number of color values found does not match the number of channels in the profile, we must throw
            if (result.ColorContext.NumChannels != values.Length)
            {
                throw new FormatException(SR.Get(SRID.Parsers_IllegalToken));
            }

            return result;
        }

        static private Color ParseScRgbColor(string trimmedColor, IFormatProvider formatProvider)
        {
            if (!trimmedColor.StartsWith("sc#", StringComparison.Ordinal))
            {
                throw new FormatException(SR.Get(SRID.Parsers_IllegalToken));
            }

            string tokens = trimmedColor.Substring(3, trimmedColor.Length - 3);

            // The tokenizer helper will tokenize a list based on the IFormatProvider.
            TokenizerHelper th = new TokenizerHelper(tokens, formatProvider);
            float[] values = new float[4];

            for (int i = 0; i < 3; i++)
            {
                values[i] = Convert.ToSingle(th.NextTokenRequired(), formatProvider);
            }

            if (th.NextToken())
            {
                values[3] = Convert.ToSingle(th.GetCurrentToken(), formatProvider);

                // We should be out of tokens at this point
                if (th.NextToken())
                {
                    throw new FormatException(SR.Get(SRID.Parsers_IllegalToken));
                }

                return Color.FromScRgb(values[0], values[1], values[2], values[3]);
            }
            else
            {
                return Color.FromScRgb(1.0f, values[0], values[1], values[2]);
            }
        }

        /// <summary>
        /// ParseColor
        /// <param name="color"> string with color description </param>
        /// <param name="formatProvider">IFormatProvider for processing string</param>
        /// </summary>
        internal static Color ParseColor(string color, IFormatProvider formatProvider)
        {
            return ParseColor(color, formatProvider, null);
        }

        /// <summary>
        /// ParseColor
        /// <param name="color"> string with color description </param>
        /// <param name="formatProvider">IFormatProvider for processing string</param>
        /// <param name="context">ITypeDescriptorContext</param>
        /// </summary>
        internal static Color ParseColor(string color, IFormatProvider formatProvider, ITypeDescriptorContext context)
        {
            bool isPossibleKnowColor;
            bool isNumericColor;
            bool isScRgbColor;
            bool isContextColor;
            string trimmedColor = KnownColors.MatchColor(color, out isPossibleKnowColor, out isNumericColor, out isContextColor, out isScRgbColor);

            if ((isPossibleKnowColor == false) &&
                (isNumericColor == false) &&
                (isScRgbColor == false) &&
                (isContextColor== false))
            {
                throw new FormatException(SR.Get(SRID.Parsers_IllegalToken));
            }

            //Is it a number?
            if (isNumericColor)
            {
                return ParseHexColor(trimmedColor);
            }
            else if (isContextColor)
            {
                return ParseContextColor(trimmedColor, formatProvider, context);
            }
            else if (isScRgbColor)
            {
                return ParseScRgbColor(trimmedColor, formatProvider);
            }
            else
            {
                KnownColor kc = KnownColors.ColorStringToKnownColor(trimmedColor);

                if (kc == KnownColor.UnknownColor)
                {
                    throw new FormatException(SR.Get(SRID.Parsers_IllegalToken));
                }

                return Color.FromUInt32((uint)kc);
            }
        }

        /// <summary>
        /// ParseBrush
        /// <param name="brush"> string with brush description </param>
        /// <param name="formatProvider">IFormatProvider for processing string</param>
        /// <param name="context">ITypeDescriptorContext</param>
        /// </summary>
        internal static Brush ParseBrush(string brush, IFormatProvider formatProvider, ITypeDescriptorContext context)
        {
            bool isPossibleKnownColor;
            bool isNumericColor;
            bool isScRgbColor;
            bool isContextColor;
            string trimmedColor = KnownColors.MatchColor(brush, out isPossibleKnownColor, out isNumericColor, out isContextColor, out isScRgbColor);

            if (trimmedColor.Length == 0)
            {
                throw new FormatException(SR.Get(SRID.Parser_Empty));
            }

            // Note that because trimmedColor is exactly brush.Trim() we don't have to worry about
            // extra tokens as we do with TokenizerHelper.  If we return one of the solid color
            // brushes then the ParseColor routine (or ColorStringToKnownColor) matched the entire
            // input.
            if (isNumericColor)
            {
                return (new SolidColorBrush(ParseHexColor(trimmedColor)));
            }

            if (isContextColor)
            {
                return (new SolidColorBrush(ParseContextColor(trimmedColor, formatProvider, context)));
            }

            if (isScRgbColor)
            {
                return (new SolidColorBrush(ParseScRgbColor(trimmedColor, formatProvider)));
            }

            if (isPossibleKnownColor)
            {
                SolidColorBrush scp = KnownColors.ColorStringToKnownBrush(trimmedColor);

                if (scp != null)
                {
                    return scp;
                }
            }

            // If it's not a color, so the content is illegal.
            throw new FormatException(SR.Get(SRID.Parsers_IllegalToken));
        }


        /// <summary>
        /// ParseTransform - parse a Transform from a string
        /// </summary>
        internal static Transform ParseTransform(
            string transformString,
            IFormatProvider formatProvider)
        {
            Matrix matrix = Matrix.Parse(transformString);

            return new MatrixTransform(matrix);
        }

        /// <summary>
        /// Parse a PathFigureCollection string.
        /// </summary>
        internal static PathFigureCollection ParsePathFigureCollection(
            string pathString,
            IFormatProvider formatProvider)
        {
            PathStreamGeometryContext context = new PathStreamGeometryContext();

            AbbreviatedGeometryParser parser = new AbbreviatedGeometryParser();

            parser.ParseToGeometryContext(context, pathString, 0 /* curIndex */);
            
            PathGeometry pathGeometry = context.GetPathGeometry();

            return pathGeometry.Figures;
        }
}
}
