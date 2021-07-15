// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
//  Contents:  Typeface metrics of a composite font
//
//


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Markup;    // for XmlLanguage
using MS.Internal.FontFace;
using System.Globalization;


namespace MS.Internal.Shaping
{
    /// <summary>
    /// Character-based info of a font family
    /// </summary>
    internal class CompositeTypefaceMetrics : ITypefaceMetrics
    {
        private double          _underlinePosition;
        private double          _underlineThickness;
        private double          _strikethroughPosition;
        private double          _strikethroughThickenss;
        private double          _capsHeight;
        private double          _xHeight;
        private FontStyle       _style;
        private FontWeight      _weight;
        private FontStretch     _stretch;


        // the following figures are collected from observation of 'Times New Roman'
        // at 72 pt in MS-Word. [wchao, 5/26/2003]

        // The following Offsets are offsets from baseline. Negative means below the baseline. 
        private const double UnderlineOffsetDefaultInEm        = -0.15625;
        private const double UnderlineSizeDefaultInEm          = (-UnderlineOffsetDefaultInEm) / 2;
        private const double StrikethroughOffsetDefaultInEm    = 0.3125;
        private const double StrikethroughSizeDefaultInEm      = UnderlineSizeDefaultInEm;
        private const double CapsHeightDefaultInEm             = 1;
        private const double XHeightDefaultInEm                = 0.671875;


        internal CompositeTypefaceMetrics(
            double      underlinePosition,
            double      underlineThickness,
            double      strikethroughPosition,
            double      strikethroughThickness,
            double      capsHeight,
            double      xHeight,
            FontStyle   style,
            FontWeight  weight,
            FontStretch stretch
            )           
        {
            _underlinePosition      = underlinePosition     != 0 ? underlinePosition      : UnderlineOffsetDefaultInEm;
            _underlineThickness     = underlineThickness     > 0 ? underlineThickness     : UnderlineSizeDefaultInEm;
            _strikethroughPosition  = strikethroughPosition != 0 ? strikethroughPosition  : StrikethroughOffsetDefaultInEm;
            _strikethroughThickenss = strikethroughThickness > 0 ? strikethroughThickness : StrikethroughSizeDefaultInEm;
            _capsHeight             = capsHeight             > 0 ? capsHeight             : CapsHeightDefaultInEm;
            _xHeight                = xHeight                > 0 ? xHeight                : XHeightDefaultInEm;
            _style                  = style;
            _weight                 = weight;
            _stretch                = stretch;
        }


        internal CompositeTypefaceMetrics()
            : this(
            UnderlineOffsetDefaultInEm,
            UnderlineSizeDefaultInEm,
            StrikethroughOffsetDefaultInEm,
            StrikethroughSizeDefaultInEm,
            CapsHeightDefaultInEm,
            XHeightDefaultInEm,
            FontStyles.Normal,
            FontWeights.Regular,
            FontStretches.Normal
            )
        {
        }

        /// <summary>
        /// (Western) x-height relative to em size.
        /// </summary>
        public double XHeight
        {
            get 
            {
                return _xHeight;
            }
        }


        /// <summary>
        /// Distance from baseline to top of English ----, relative to em size.
        /// </summary>
        public double CapsHeight
        {
            get 
            {
                return _capsHeight;
            }
        }


        /// <summary>
        /// Distance from baseline to underline position
        /// </summary>
        public double UnderlinePosition
        {
            get 
            {
                return _underlinePosition;
            }
        }


        /// <summary>
        /// Underline thickness
        /// </summary>
        public double UnderlineThickness
        {
            get
            {
                return _underlineThickness;
            }
        }


        /// <summary>
        /// Distance from baseline to strike-through position
        /// </summary>
        public double StrikethroughPosition
        {
            get 
            {
                return _strikethroughPosition;
            }
        }


        /// <summary>
        /// strike-through thickness
        /// </summary>
        public double StrikethroughThickness
        {
            get
            {
                return _strikethroughThickenss;
            }
        }


        /// <summary>
        /// Flag indicate whether this is symbol typeface
        /// </summary>
        public bool Symbol
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Style simulation flags for this typeface.
        /// </summary>
        public StyleSimulations StyleSimulations
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
    }
}
