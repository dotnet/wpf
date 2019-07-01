// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;


namespace Microsoft.Test.Text
{
    /// <summary>
    /// PropertyFactory class stores string property objects
    /// </summary>
    internal class PropertyFactory
    {
        private BidiProperty bidiProperty;
        private CombiningMarksProperty combiningMarksProperty;
        private EudcProperty eudcProperty;
        private LineBreakProperty lineBreakProperty;
        private NumberProperty numberProperty;
        private SurrogatePairProperty surrogatePairProperty;
        private TextNormalizationProperty textNormalizationProperty;
        private TextSegmentationProperty textSegmentationProperty;

        /// <summary>
        /// Enum all available properties
        /// </summary>
        public enum PropertyName
        {
            /// <summary>
            /// Bidi property
            /// </summary>
            Bidi = 0,

            /// <summary>
            /// Combining mark
            /// </summary>
            CombiningMarks,

            /// <summary>
            /// EUDC
            /// </summary>
            Eudc,

            /// <summary>
            /// Line break
            /// </summary>
            LineBreak,

            /// <summary>
            /// Number
            /// </summary>
            Number,

            /// <summary>
            /// Surrogate
            /// </summary>
            Surrogate,

            /// <summary>
            /// Text normalization
            /// </summary>
            TextNormalization,

            /// <summary>
            /// Text segmentation
            /// </summary>
            TextSegmentation
        }

        private int minNumOfCodePoint;

        private Dictionary<PropertyFactory.PropertyName, IStringProperty> propertyDictionary;

        /// <summary>
        /// Create property objects according to string properties
        /// </summary>
        public PropertyFactory(StringProperties properties, UnicodeRangeDatabase unicodeDb, Collection<UnicodeRange> expectedRanges)
        {
            bidiProperty = null;
            combiningMarksProperty = null;
            eudcProperty = null;
            lineBreakProperty = null;
            numberProperty = null;
            surrogatePairProperty = null;
            textNormalizationProperty = null;
            textSegmentationProperty = null;
            minNumOfCodePoint = 0;
            propertyDictionary = new Dictionary<PropertyFactory.PropertyName, IStringProperty>();
            CreateProperties(properties, unicodeDb, expectedRanges);
        }

        /// <summary>
        /// Check if property object exists
        /// </summary>
        public bool HasProperty(PropertyName propertyName)
        {
            if (PropertyName.Bidi == propertyName)
            {
                return null == bidiProperty ? false : true;
            }
            else if (PropertyName.CombiningMarks == propertyName)
            {
                return null == combiningMarksProperty ? false : true;
            }
            else if (PropertyName.Eudc == propertyName)
            {
                return null == eudcProperty ? false : true;
            }
            else if (PropertyName.LineBreak == propertyName)
            {
                return null == lineBreakProperty ? false : true;
            }
            else if (PropertyName.Number == propertyName)
            {
                return null == numberProperty ? false : true;
            }
            else if (PropertyName.Surrogate == propertyName)
            {
                return null == surrogatePairProperty ? false : true;
            }
            else if (PropertyName.TextNormalization == propertyName)
            {
                return null == textNormalizationProperty ? false : true;
            }
            else if (PropertyName.TextSegmentation == propertyName)
            {
                return null == textSegmentationProperty ? false : true;
            }
            else
            {
                return false;
            }
        }

        private void CreateProperties(StringProperties properties, UnicodeRangeDatabase unicodeDb, Collection<UnicodeRange> expectedRanges)
        {
            if (null != properties.HasNumbers)
            {
                if ((bool)properties.HasNumbers)
                {
                    numberProperty = new NumberProperty(unicodeDb, expectedRanges);
                    minNumOfCodePoint += NumberProperty.MINNUMOFCODEPOINT;
                    propertyDictionary.Add(PropertyName.Number, numberProperty);
                }
            }

            if (null != properties.IsBidirectional)
            {
                if ((bool)properties.IsBidirectional)
                {
                    bidiProperty = new BidiProperty(unicodeDb, expectedRanges);
                    minNumOfCodePoint += BidiProperty.MINNUMOFCODEPOINT;
                    propertyDictionary.Add(PropertyName.Bidi, bidiProperty);
                }
            }

            if (null != properties.NormalizationForm)
            {
                textNormalizationProperty = new TextNormalizationProperty(unicodeDb, expectedRanges);
                minNumOfCodePoint += TextNormalizationProperty.MINNUMOFCODEPOINT;
                propertyDictionary.Add(PropertyName.TextNormalization, textNormalizationProperty);
            }

            if (null != properties.MinNumberOfCombiningMarks)
            {
                if (0 != properties.MinNumberOfCombiningMarks)
                {
                    combiningMarksProperty = new CombiningMarksProperty(unicodeDb, expectedRanges);
                    minNumOfCodePoint += CombiningMarksProperty.MINNUMOFCODEPOINT * (int)properties.MinNumberOfCombiningMarks;
                    propertyDictionary.Add(PropertyName.CombiningMarks, combiningMarksProperty);
                }
            }

            if (null != properties.MinNumberOfEndUserDefinedCodePoints)
            {
                if (0 != properties.MinNumberOfEndUserDefinedCodePoints)
                {
                    eudcProperty = new EudcProperty(unicodeDb, expectedRanges);
                    minNumOfCodePoint += EudcProperty.MINNUMOFCODEPOINT * (int)properties.MinNumberOfEndUserDefinedCodePoints;
                    propertyDictionary.Add(PropertyName.Eudc, eudcProperty);
                }
            }

            if (null != properties.MinNumberOfLineBreaks)
            {
                if (0 != properties.MinNumberOfLineBreaks)
                {
                    lineBreakProperty = new LineBreakProperty(expectedRanges);
                    minNumOfCodePoint += LineBreakProperty.MINNUMOFCODEPOINT * (int)properties.MinNumberOfLineBreaks;
                    propertyDictionary.Add(PropertyName.LineBreak, lineBreakProperty);
                }
            }

            if (null != properties.MinNumberOfSurrogatePairs)
            {
                if (0 != properties.MinNumberOfSurrogatePairs)
                {
                    surrogatePairProperty = new SurrogatePairProperty(unicodeDb, expectedRanges);
                    minNumOfCodePoint += SurrogatePairProperty.MINNUMOFCODEPOINT * (int)properties.MinNumberOfSurrogatePairs;
                    propertyDictionary.Add(PropertyName.Surrogate, surrogatePairProperty);
                }
            }

            if (null != properties.MinNumberOfTextSegmentationCodePoints)
            {
                if (0 != properties.MinNumberOfTextSegmentationCodePoints)
                {
                    textSegmentationProperty = new TextSegmentationProperty(unicodeDb, expectedRanges);
                    minNumOfCodePoint += TextSegmentationProperty.MINNUMOFCODEPOINT * (int)properties.MinNumberOfTextSegmentationCodePoints;
                    propertyDictionary.Add(PropertyName.TextSegmentation, textSegmentationProperty);
                }
            }
        }

        /// <summary>
        /// Minimum number of code points needed to have to cover non-null properties
        /// </summary>
        public int MinNumOfCodePoint { get {return minNumOfCodePoint; } }

        /// <summary>
        /// Create property objects according to string properties
        /// </summary>
        public Dictionary<PropertyFactory.PropertyName, IStringProperty> PropertyDictionary { get { return propertyDictionary; } }
    }
}



