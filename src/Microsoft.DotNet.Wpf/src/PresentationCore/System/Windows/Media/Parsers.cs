// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//  Synopsis: Implements class Parsers for internal use of type converters

using System.ComponentModel;
using System.Windows.Media;

namespace MS.Internal
{
    internal static partial class Parsers
    {
        private const int s_zeroChar = (int) '0';
        private const int s_aLower   = (int) 'a';
        private const int s_aUpper   = (int) 'A';

        private static int ParseHexChar(char c )
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
            throw new FormatException(SR.Parsers_IllegalToken);
        }

        static private Color ParseHexColor(ReadOnlySpan<char> trimmedColor)
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

        internal const string ContextColor = "ContextColor ";

        private static Color ParseContextColor(ReadOnlySpan<char> trimmedColor, IFormatProvider formatProvider, ITypeDescriptorContext context)
        {
            if (!trimmedColor.StartsWith(ContextColor, StringComparison.OrdinalIgnoreCase))
                throw new FormatException(SR.Parsers_IllegalToken);

            // Skip "ContextColor " prefix
            ReadOnlySpan<char> tokens = trimmedColor.Slice(ContextColor.Length).Trim();

            // Check whether the format is at least e.g. "file://profile.icc 1.0"
            Span<Range> splitSegments = stackalloc Range[4];

            if (tokens.Split(splitSegments, ' ') < 2)
                throw new FormatException(SR.Parsers_IllegalToken);

            // Retrieve "file://profile.icc" part
            string profileString = tokens[splitSegments[0]].ToString();
            // Skip "file://profile.icc" part
            ReadOnlySpan<char> colorPart = tokens.Slice(profileString.Length);

            // Retrieve alpha value
            ValueTokenizerHelper tokenizer = new(colorPart, formatProvider);
            float alpha = float.Parse(tokenizer.NextTokenRequired(), formatProvider);

            // While we do not support colors with more than 8 channels, the underlying initialization code will take care of it,
            // so here we just silently count the color values and let it throw NotImplementedException in the color translation code-path.
            int numTokens = colorPart.Count(',');
            Span<float> values = stackalloc float[numTokens];

            for (int i = 0; i < values.Length; i++)
                values[i] = float.Parse(tokenizer.NextTokenRequired(), formatProvider);

            UriHolder uriHolder = TypeConverterHelper.GetUriFromUriContext(context, profileString);
            Uri profileUri = uriHolder.BaseUri is not null ? new Uri(uriHolder.BaseUri, uriHolder.OriginalUri) : uriHolder.OriginalUri;

            // If the number of color values found does not match the number of channels in the profile, FromAValues will throw
            return Color.FromAValues(alpha, values, profileUri);
        }

        private static Color ParseScRgbColor(ReadOnlySpan<char> trimmedColor, IFormatProvider formatProvider)
        {
            if (!trimmedColor.StartsWith("sc#", StringComparison.Ordinal))
                throw new FormatException(SR.Parsers_IllegalToken);

            // Skip prefix (sc#)
            ReadOnlySpan<char> tokens = trimmedColor.Slice(3);

            // The tokenizer helper will tokenize a list based on the IFormatProvider.
            ValueTokenizerHelper tokenizer = new(tokens, formatProvider);
            Span<float> values = stackalloc float[4];

            for (int i = 0; i < 3; i++)
                values[i] = float.Parse(tokenizer.NextTokenRequired(), formatProvider);

            if (tokenizer.NextToken())
            {
                values[3] = float.Parse(tokenizer.GetCurrentToken(), formatProvider);

                // We should be out of tokens at this point
                if (tokenizer.NextToken())
                {
                    throw new FormatException(SR.Parsers_IllegalToken);
                }

                return Color.FromScRgb(values[0], values[1], values[2], values[3]);
            }

            return Color.FromScRgb(1.0f, values[0], values[1], values[2]);
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
            ReadOnlySpan<char> trimmedColor = color.AsSpan().Trim();
            ColorKind colorKind = KnownColors.MatchColor(trimmedColor);

            // Check that our assumption stays true
            Debug.Assert(colorKind is ColorKind.NumericColor or ColorKind.ContextColor or ColorKind.ScRgbColor or ColorKind.KnownColor);

            if (colorKind is ColorKind.NumericColor)
                return ParseHexColor(trimmedColor);

            if (colorKind is ColorKind.ContextColor)
                return ParseContextColor(trimmedColor, formatProvider, context);

            if (colorKind is ColorKind.ScRgbColor)
                return ParseScRgbColor(trimmedColor, formatProvider);

            KnownColor knownColor = KnownColors.ColorStringToKnownColor(trimmedColor);
            if (knownColor == KnownColor.UnknownColor)
                throw new FormatException(SR.Parsers_IllegalToken);

            return Color.FromUInt32((uint)knownColor);
        }

        /// <summary>
        /// ParseBrush
        /// <param name="brush"> string with brush description </param>
        /// <param name="formatProvider">IFormatProvider for processing string</param>
        /// <param name="context">ITypeDescriptorContext</param>
        /// </summary>
        internal static Brush ParseBrush(string brush, IFormatProvider formatProvider, ITypeDescriptorContext context)
        {
            ReadOnlySpan<char> trimmedColor = brush.AsSpan().Trim();
            if (trimmedColor.IsEmpty)
                throw new FormatException(SR.Parser_Empty);

            ColorKind colorKind = KnownColors.MatchColor(trimmedColor);

            // Check that our assumption stays true
            Debug.Assert(colorKind is ColorKind.NumericColor or ColorKind.ContextColor or ColorKind.ScRgbColor or ColorKind.KnownColor);

            // Note that because trimmedColor is exactly brush.Trim() we don't have to worry about
            // extra tokens as we do with TokenizerHelper. If we return one of the solid color brushes
            // then the ParseColor routine (or ColorStringToKnownColor) matched the entire input.
            if (colorKind is ColorKind.NumericColor)
                return new SolidColorBrush(ParseHexColor(trimmedColor));

            if (colorKind is ColorKind.ContextColor)
                return new SolidColorBrush(ParseContextColor(trimmedColor, formatProvider, context));

            if (colorKind is ColorKind.ScRgbColor)
                return new SolidColorBrush(ParseScRgbColor(trimmedColor, formatProvider));

            // NULL is returned when the color was not valid
            SolidColorBrush solidColorBrush = KnownColors.ColorStringToKnownBrush(trimmedColor);

            return solidColorBrush is not null ? solidColorBrush : throw new FormatException(SR.Parsers_IllegalToken);
        }


        /// <summary>
        /// ParseTransform - parse a Transform from a string
        /// </summary>
        internal static Transform ParseTransform(string transformString, IFormatProvider formatProvider)
        {
            Matrix matrix = Matrix.Parse(transformString);

            return new MatrixTransform(matrix);
        }

        /// <summary>
        /// Parse a PathFigureCollection string.
        /// </summary>
        internal static PathFigureCollection ParsePathFigureCollection(string pathString, IFormatProvider formatProvider)
        {
            PathStreamGeometryContext context = new();

            AbbreviatedGeometryParser parser = new();

            parser.ParseToGeometryContext(context, pathString, startIndex: 0);
            
            PathGeometry pathGeometry = context.GetPathGeometry();

            return pathGeometry.Figures;
        }
}
}
