// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
//  Contents:  CharacterMetrics
//
//

using System;
using System.Globalization;
using StringBuilder = System.Text.StringBuilder;
using CompositeFontParser = MS.Internal.FontFace.CompositeFontParser;
using Constants = MS.Internal.TextFormatting.Constants;
using SR = MS.Internal.PresentationCore.SR;
using SRID = MS.Internal.PresentationCore.SRID;

#pragma warning disable 1634, 1691  // suppressing PreSharp warnings

namespace System.Windows.Media
{
    /// <summary>
    /// Metrics used to lay out a character in a device font.
    /// </summary>
    public class CharacterMetrics
    {
        private double _blackBoxWidth;
        private double _blackBoxHeight;
        private double _baseline;
        private double _leftSideBearing;
        private double _rightSideBearing;
        private double _topSideBearing;
        private double _bottomSideBearing;

        private enum FieldIndex
        {
            BlackBoxWidth,
            BlackBoxHeight,
            Baseline,
            LeftSideBearing,
            RightSideBearing,
            TopSideBearing,
            BottomSideBearing
        }

        private const int NumFields = (int)FieldIndex.BottomSideBearing + 1;
        private const int NumRequiredFields = (int)FieldIndex.BlackBoxHeight + 1;

        /// <summary>
        /// Constructs a CharacterMetrics object with default values.
        /// </summary>
        public CharacterMetrics()
        {
        }

        /// <summary>
        /// Constructs a CharacterMetrics with the specified values.
        /// </summary>
        /// <param name="metrics">Value of the Metrics property.</param>
        public CharacterMetrics(string metrics)
        {
            if (metrics == null)
                throw new ArgumentNullException("metrics");
            Metrics = metrics;
        }

        /// <summary>
        /// String specifying the following properties in the following order: BlackBoxWidth, BlackBoxHeight,
        /// Baseline, LeftSideBearing, RightSideBearing, TopSideBearing, BottomSideBearing. Property values 
        /// are delimited by commas, and the first two properties are required. The remaining properties may 
        /// be omitted and default to zero. For example, "0.75,0.75,,0.1" sets the first, second, and fourth
        /// properties to the specified values and the rest to zero.
        /// </summary>
        public string Metrics
        {
            get
            {
                StringBuilder s = new StringBuilder();

                // The following fields are required.
                s.Append(_blackBoxWidth.ToString(System.Windows.Markup.TypeConverterHelper.InvariantEnglishUS));
                s.Append(',');
                s.Append(_blackBoxHeight.ToString(System.Windows.Markup.TypeConverterHelper.InvariantEnglishUS));

                // Index of the last field we added to the string; this tells us how many commas to
                // insert before the next optional field we add.
                int lastIndex = (int)FieldIndex.BlackBoxHeight;

                // The following fields are optional, but must be in ascending order of field index.
                AppendField(_baseline, FieldIndex.Baseline, ref lastIndex, s);
                AppendField(_leftSideBearing, FieldIndex.LeftSideBearing, ref lastIndex, s);
                AppendField(_rightSideBearing, FieldIndex.RightSideBearing, ref lastIndex, s);
                AppendField(_topSideBearing, FieldIndex.TopSideBearing, ref lastIndex, s);
                AppendField(_bottomSideBearing, FieldIndex.BottomSideBearing, ref lastIndex, s);

                return s.ToString();
            }

            set
            {
                double[] metrics = ParseMetrics(value);

                // Validate all the values before we assign to any field.
                CompositeFontParser.VerifyNonNegativeMultiplierOfEm("BlackBoxWidth", ref metrics[(int)FieldIndex.BlackBoxWidth]);
                CompositeFontParser.VerifyNonNegativeMultiplierOfEm("BlackBoxHeight", ref metrics[(int)FieldIndex.BlackBoxHeight]);
                CompositeFontParser.VerifyMultiplierOfEm("Baseline", ref metrics[(int)FieldIndex.Baseline]);
                CompositeFontParser.VerifyMultiplierOfEm("LeftSideBearing", ref metrics[(int)FieldIndex.LeftSideBearing]);
                CompositeFontParser.VerifyMultiplierOfEm("RightSideBearing", ref metrics[(int)FieldIndex.RightSideBearing]);
                CompositeFontParser.VerifyMultiplierOfEm("TopSideBearing", ref metrics[(int)FieldIndex.TopSideBearing]);
                CompositeFontParser.VerifyMultiplierOfEm("BottomSideBearing", ref metrics[(int)FieldIndex.BottomSideBearing]);

                double horizontalAdvance = metrics[(int)FieldIndex.BlackBoxWidth]
                    + metrics[(int)FieldIndex.LeftSideBearing]
                    + metrics[(int)FieldIndex.RightSideBearing];
                if (horizontalAdvance < 0)
                    throw new ArgumentException(SR.Get(SRID.CharacterMetrics_NegativeHorizontalAdvance));

                double verticalAdvance = metrics[(int)FieldIndex.BlackBoxHeight]
                    + metrics[(int)FieldIndex.TopSideBearing]
                    + metrics[(int)FieldIndex.BottomSideBearing];
                if (verticalAdvance < 0)
                    throw new ArgumentException(SR.Get(SRID.CharacterMetrics_NegativeVerticalAdvance));

                // Set all the properties.
                _blackBoxWidth = metrics[(int)FieldIndex.BlackBoxWidth];
                _blackBoxHeight = metrics[(int)FieldIndex.BlackBoxHeight];
                _baseline = metrics[(int)FieldIndex.Baseline];
                _leftSideBearing = metrics[(int)FieldIndex.LeftSideBearing];
                _rightSideBearing = metrics[(int)FieldIndex.RightSideBearing];
                _topSideBearing = metrics[(int)FieldIndex.TopSideBearing];
                _bottomSideBearing = metrics[(int)FieldIndex.BottomSideBearing];
            }
        }

        private static void AppendField(double value, FieldIndex fieldIndex, ref int lastIndex, StringBuilder s)
        {
            if (value != 0)
            {
                s.Append(',', (int)fieldIndex - lastIndex);
                lastIndex = (int)fieldIndex;
                s.Append(value.ToString(System.Windows.Markup.TypeConverterHelper.InvariantEnglishUS));
            }
        }

        private static double[] ParseMetrics(string s)
        {
            double[] metrics = new double[NumFields];

            int i = 0, fieldIndex = 0;
            for (; ; )
            {
                // Let i be first non-whitespace character or end-of-string.
                while (i < s.Length && s[i] == ' ')
                    ++i;

                // Let j be delimiter or end-of-string.
                int j = i;
                while (j < s.Length && s[j] != ',')
                    ++j;

                // Let k be end-of-field without trailing whitespace.
                int k = j;
                while (k > i && s[k - 1] == ' ')
                    --k;

                if (k > i)
                {
                    // Non-empty field; convert it to double.
                    string field = s.Substring(i, k - i);
                    if (!double.TryParse(
                        field,
                        NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
                        System.Windows.Markup.TypeConverterHelper.InvariantEnglishUS,
                        out metrics[fieldIndex]
                        ))
                    {
                        throw new ArgumentException(SR.Get(SRID.CannotConvertStringToType, field, "double"));
                    }
                }
                else if (fieldIndex < NumRequiredFields)
                {
                    // Empty field; make sure it's an optional one.
                    throw new ArgumentException(SR.Get(SRID.CharacterMetrics_MissingRequiredField));
                }

                ++fieldIndex;

                if (j < s.Length)
                {
                    // There's a comma so check if we've exceeded the number of fields.
                    if (fieldIndex == NumFields)
                        throw new ArgumentException(SR.Get(SRID.CharacterMetrics_TooManyFields));

                    // Initialize character index for next iteration.
                    i = j + 1;
                }
                else
                {
                    // No more fields; check if we have all required fields.
                    if (fieldIndex < NumRequiredFields)
                    {
                        throw new ArgumentException(SR.Get(SRID.CharacterMetrics_MissingRequiredField));
                    }

                    break;
                }
            }

            return metrics;
        }


        /// <summary>
        /// Width of the black box for the character.
        /// </summary>
        public double BlackBoxWidth
        {
            get { return _blackBoxWidth; }
        }
        
        /// <summary>
        /// Height of the black box for the character.
        /// </summary>
        public double BlackBoxHeight
        {
            get { return _blackBoxHeight; }
        }

        /// <summary>
        /// Vertical offset from the bottom of the black box to the baseline. A positive
        /// value indicates the baseline is above the bottom of the black box, and a negative
        /// value indicates the baseline is below the bottom of the black box.
        /// </summary>
        public double Baseline
        {
            get { return _baseline; }
        }

        /// <summary>
        /// Recommended white space to the left of the black box. A negative value results in
        /// an overhang. The horizontal advance for the character is LeftSideBearing +
        /// BlackBoxWidth + RightSideBearing, and cannot be less than zero.
        /// </summary>
        public double LeftSideBearing
        {
            get { return _leftSideBearing; }
        }

        /// <summary>
        /// Recommended white space to the right of the black box. A negative value results in
        /// an overhang. The horizontal advance for the character is LeftSideBearing +
        /// BlackBoxWidth + RightSideBearing, and cannot be less than zero.
        /// </summary>
        public double RightSideBearing
        {
            get { return _rightSideBearing; }
        }

        /// <summary>
        /// Recommended white space above the black box. A negative value results in
        /// an overhang. The vertical advance for the character is TopSideBearing +
        /// BlackBoxHeight + BottomSideBearing, and cannot be less than zero.
        /// </summary>
        public double TopSideBearing
        {
            get { return _topSideBearing; }
        }

        /// <summary>
        /// Recommended white space below the black box. A negative value results in
        /// an overhang. The vertical advance for the character is TopSideBearing +
        /// BlackBoxHeight + BottomSideBearing, and cannot be less than zero.
        /// </summary>
        public double BottomSideBearing
        {
            get { return _bottomSideBearing; }
        }


        /// <summary>
        /// Compares two CharacterMetrics for equality.
        /// </summary>
        public override bool Equals(object obj)
        {
            CharacterMetrics other = obj as CharacterMetrics;

            // Suppress PRESharp warning that other can be null; apparently PRESharp
            // doesn't understand short circuit evaluation of operator &&.
            #pragma warning disable 6506
            return other != null &&
                other._blackBoxWidth == _blackBoxWidth &&
                other._blackBoxHeight == _blackBoxHeight &&
                other._leftSideBearing == _leftSideBearing &&
                other._rightSideBearing == _rightSideBearing &&
                other._topSideBearing == _topSideBearing &&
                other._bottomSideBearing == _bottomSideBearing;
            #pragma warning restore 6506
        }

        /// <summary>
        /// Computes a hash value for a CharacterMetrics.
        /// </summary>
        public override int GetHashCode()
        {
            int hash = (int)(_blackBoxWidth * Constants.DefaultRealToIdeal);
            hash = (hash * HashMultiplier) + (int)(_blackBoxHeight * Constants.DefaultRealToIdeal);
            hash = (hash * HashMultiplier) + (int)(_baseline * Constants.DefaultRealToIdeal);
            hash = (hash * HashMultiplier) + (int)(_leftSideBearing * Constants.DefaultRealToIdeal);
            hash = (hash * HashMultiplier) + (int)(_rightSideBearing * Constants.DefaultRealToIdeal);
            hash = (hash * HashMultiplier) + (int)(_topSideBearing * Constants.DefaultRealToIdeal);
            hash = (hash * HashMultiplier) + (int)(_bottomSideBearing * Constants.DefaultRealToIdeal);
            return hash;
        }

        private const int HashMultiplier = 101;
    }
}
