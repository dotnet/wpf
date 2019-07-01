// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// A factory which create AccessText.
    /// </summary>
    [TargetTypeAttribute(typeof(AccessText))]
    internal class AccessTextFactory : DiscoverableFactory<AccessText>
    {
        #region Public Members

        /// <summary>
        /// Gets or sets a Brush to set AccessText Background property.
        /// </summary>
        public Brush Background { get; set; }

        /// <summary>
        /// Gets or sets a FontFamily to set AccessText FontFamily property.
        /// </summary>
        public FontFamily FontFamily { get; set; }

        /// <summary>
        /// Gets or sets a Brush to set AccessText Foreground property.
        /// </summary>
        public Brush Foreground { get; set; }

        /// <summary>
        /// Gets or sets a string to set AccessText Text property.
        /// </summary>
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public String Text { get; set; }

        /// <summary>
        /// Gets or sets a value to set AccessText FontSize property.
        /// </summary>
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public double FontSize { get; set; }

        /// <summary>
        /// Gets or sets a TextDecorationCollection to set AccessText TextDecorations property.
        /// </summary>
        public TextDecorationCollection TextDecorations { get; set; }

        /// <summary>
        /// Gets or sets a TextEffectCollection to set AccessText TextEffects property.
        /// </summary>
        public TextEffectCollection TextEffects { get; set; }

        #endregion

        #region Override Members

        /// <summary>
        /// Create an AccessText.
        /// </summary>
        /// <param name="random"></param>
        /// <returns></returns>
        public override AccessText Create(DeterministicRandom random)
        {
            AccessText accessText = new AccessText();

            accessText.Background = Background;
            //Set BaselineOffset value [-10000, 10000).
            double baselineOffset = (random.NextDouble() - 0.5) * 20000;
            accessText.BaselineOffset = baselineOffset;
            accessText.FontFamily = FontFamily;
            accessText.FontSize = FontSize;
            accessText.FontStretch = random.NextStaticProperty<FontStretch>(typeof(FontStretches));
            accessText.FontStyle = random.NextStaticProperty<FontStyle>(typeof(FontStyles));
            accessText.FontWeight = random.NextStaticProperty<FontWeight>(typeof(FontWeights));
            accessText.Foreground = Foreground;
            accessText.LineHeight = GenerateLineHeightValue(random);
            accessText.LineStackingStrategy = random.NextEnum<LineStackingStrategy>();
            accessText.Text = Text;
            accessText.TextAlignment = random.NextEnum<TextAlignment>();
            accessText.TextDecorations = TextDecorations;
            accessText.TextEffects = TextEffects;
            accessText.TextTrimming = random.NextEnum<TextTrimming>();
            accessText.TextWrapping = random.NextEnum<TextWrapping>();

            return accessText;
        }

        #endregion

        #region Private Members

        /// <summary>
        /// //LineHeight value must be [0.0034, 160000] or Double.NaN
        /// </summary>
        /// <param name="random"></param>
        /// <returns></returns>
        private double GenerateLineHeightValue(DeterministicRandom random)
        {
            double lineHeight = random.NextDouble() * 200000;
            if (lineHeight > 160000)
            {
                lineHeight = Double.NaN;
            }

            if (lineHeight < 0.0034)
            {
                lineHeight = 0.0034;
            }

            return lineHeight;
        }

        #endregion
    }
}
