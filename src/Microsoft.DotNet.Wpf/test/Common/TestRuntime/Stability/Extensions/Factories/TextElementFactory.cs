// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    abstract class TextElementFactory<T> : DiscoverableFactory<T> where T : TextElement
    {
        public Brush Background { get; set; }
        public Brush Foreground { get; set; }
        public FontFamily FontFamily { get; set; }
        public TextEffectCollection TextEffects { get; set; }
        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public Double FontSize { get; set; }

        protected void ApplyTextElementFactory(T textElement, DeterministicRandom random)
        {
            textElement.Background = Background;

            if (FontFamily != null)
            {
                textElement.FontFamily = FontFamily;
            }

            textElement.FontSize = FontSize;
            textElement.FontStretch = random.NextStaticProperty<FontStretch>(typeof(FontStretches));
            textElement.FontStyle = random.NextStaticProperty<FontStyle>(typeof(FontStyles));
            textElement.FontWeight = random.NextStaticProperty<FontWeight>(typeof(FontWeights));
            textElement.Foreground = Foreground;
            textElement.TextEffects = TextEffects;
        }
    }
}
