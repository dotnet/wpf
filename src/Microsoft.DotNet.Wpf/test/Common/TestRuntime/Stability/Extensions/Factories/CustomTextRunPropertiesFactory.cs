// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// This Factory is used for creating CustomTextRunProperties objects, which provide
    /// a set of properties that can be applied to a TextRun object.
    /// 
    /// This factory has a dependency on \\wpf\testscratch\TextTest\StressDataFiles\FontLists\
    /// and \\wpf\testscratch\TextTest where a set of custom fonts reside.
    /// </summary>
    [TargetTypeAttribute(typeof(CustomTextRunProperties))]
    public class CustomTextRunPropertiesFactory : DiscoverableFactory<CustomTextRunProperties>
    {
        private enum FontType { SystemFont, CustomFont };

        public FontFamily CustomFontFamily { get; set; }

        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public double FontSize { get; set; }

        public Brush ForegroundColor { get; set; }
        public Brush BackgroundColor { get; set; }

        public CultureInfo CultureInformation { get; set; }

        /// <summary>
        /// The constructor which randomly selects FontSize, FontFamily, FontStyle, FontWeight
        /// FontStretch, BaselineAlignment, and CultureInfo.
        /// </summary>
        public override CustomTextRunProperties Create(DeterministicRandom random)
        {
            TextDecorationCollection textDecorationCollection = random.NextStaticProperty<TextDecorationCollection>(typeof(TextDecorations));

            int fontIndex;
            FontFamily fontFamily = null;
            FontType fontType = random.NextEnum<FontType>();
            switch (fontType)
            {
                case FontType.SystemFont:
                    fontIndex = random.Next(Fonts.SystemFontFamilies.Count);
                    /*
                     * Note: If we are to use System.Linq namespace, the following few lines can be replaced by:
                     *      
                     *               fontFamily = Fonts.SystemFontFamilies.ElementAt(fontIndex);
                     * 
                     * System.Linq cannot be used by TestRuntime and we would have to resort to other workarounds.
                     * Since this is the only place that this code is used, we'll keep it as is.
                     */
                    IEnumerator<FontFamily> ie = Fonts.SystemFontFamilies.GetEnumerator();
                    ie.Reset();
                    while (fontIndex >= 0)
                    {
                        ie.MoveNext();
                        fontIndex--;
                    }
                    fontFamily = ie.Current;
                    break;

                case FontType.CustomFont:
                    fontFamily = CustomFontFamily;
                    break;
            }

            if (fontFamily == null) // this should not happen, but just in case
            {
                fontFamily = new FontFamily("Arial");
            }

            FontStyle fontStyle = random.NextStaticProperty<FontStyle>(typeof(FontStyles));
            FontWeight fontWeight = random.NextStaticProperty<FontWeight>(typeof(FontWeights));
            FontStretch fontStretch = random.NextStaticProperty<FontStretch>(typeof(FontStretches));
            Typeface typeFace = new Typeface(fontFamily, fontStyle, fontWeight, fontStretch);
            BaselineAlignment bAlignment = random.NextEnum<BaselineAlignment>();

            CustomTextRunProperties textRunProperties = new CustomTextRunProperties(typeFace, FontSize, FontSize, textDecorationCollection,
                                                                                    ForegroundColor, BackgroundColor, bAlignment,
                                                                                    CultureInformation);

            return textRunProperties;
        }
    }

    /// <summary>
    /// Class used to implement TextRunProperties which provides a set of properties, 
    /// such as typeface or foreground brush, that can be applied to a TextRun object.
    /// </summary>
    public class CustomTextRunProperties : TextRunProperties
    {
        #region Constructor

        public CustomTextRunProperties(Typeface typeface, double size, double hintingSize, TextDecorationCollection textDecorations,
                                        Brush forgroundBrush, Brush backgroundBrush, BaselineAlignment baselineAlignment, CultureInfo culture)
        {
            if (typeface == null)
                throw new ArgumentNullException("typeface");

            if (culture == null)
                throw new ArgumentNullException("culture");

            if (size <= 0)
                throw new ArgumentOutOfRangeException("size", "Parameter Must Be Greater Than Zero.");
            if (double.IsNaN(size))
                throw new ArgumentOutOfRangeException("size", "Parameter Cannot Be NaN.");

            this.typeface = typeface;
            emSize = size;
            emHintingSize = hintingSize;
            this.textDecorations = textDecorations;
            this.foregroundBrush = forgroundBrush;
            this.backgroundBrush = backgroundBrush;
            this.baselineAlignment = baselineAlignment;
            this.culture = culture;
        }

        #endregion

        #region Properties

        public override Typeface Typeface
        {
            get { return typeface; }
        }

        public override double FontRenderingEmSize
        {
            get { return emSize; }
        }

        public override double FontHintingEmSize
        {
            get { return emHintingSize; }
        }

        public override TextDecorationCollection TextDecorations
        {
            get { return textDecorations; }
        }

        public override Brush ForegroundBrush
        {
            get { return foregroundBrush; }
        }

        public override Brush BackgroundBrush
        {
            get { return backgroundBrush; }
        }

        public override BaselineAlignment BaselineAlignment
        {
            get { return baselineAlignment; }
        }

        public override CultureInfo CultureInfo
        {
            get { return culture; }
        }

        public override TextRunTypographyProperties TypographyProperties
        {
            get { return null; }
        }

        public override TextEffectCollection TextEffects
        {
            get { return null; }
        }

        public override NumberSubstitution NumberSubstitution
        {
            get { return null; }
        }

        #endregion

        #region Private Fields

        private Typeface typeface;
        private double emSize;
        private double emHintingSize;
        private TextDecorationCollection textDecorations;
        private Brush foregroundBrush;
        private Brush backgroundBrush;
        private BaselineAlignment baselineAlignment;
        private CultureInfo culture;

        #endregion
    }
}
