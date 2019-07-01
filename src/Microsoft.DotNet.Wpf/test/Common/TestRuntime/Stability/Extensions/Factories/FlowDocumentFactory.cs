// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    class FlowDocumentFactory : DiscoverableFactory<FlowDocument>
    {
        public Brush Background { get; set; }
        public Double ColumnGap { get; set; }
        public Brush ColumnRuleBrush { get; set; }
        public FontFamily FontFamily { get; set; }
        public Brush Foreground { get; set; }
        public Boolean IsColumnWidthFlexible { get; set; }
        public Boolean IsHyphenationEnabled { get; set; }
        public Boolean IsOptimalParagraphEnabled { get; set; }
        public Thickness Thickness { get; set; }
        public TextEffectCollection TextEffectCollection { get; set; }
        public List<Block> Children { get; set; }

        public override FlowDocument Create(DeterministicRandom random)
        {
            FlowDocument flowDocument = new FlowDocument();

            HomelessTestHelpers.Merge(flowDocument.Blocks, Children);
            flowDocument.Background = Background;
            flowDocument.ColumnGap = random.NextDouble() * 10;
            flowDocument.ColumnRuleBrush = ColumnRuleBrush;
            flowDocument.ColumnRuleWidth = random.NextDouble() * 10;
            flowDocument.ColumnWidth = random.NextDouble() * 100;
            flowDocument.FlowDirection = random.NextEnum<FlowDirection>();

            if (FontFamily != null)
            {
                flowDocument.FontFamily = FontFamily;
            }

            flowDocument.FontSize = random.NextDouble() * 20 + 0.1;
            flowDocument.FontStretch = random.NextStaticProperty<FontStretch>(typeof(FontStretches));
            flowDocument.FontStyle = random.NextStaticProperty<FontStyle>(typeof(FontStyles));
            flowDocument.FontWeight = random.NextStaticProperty<FontWeight>(typeof(FontWeights));
            flowDocument.Foreground = Foreground;
            flowDocument.IsColumnWidthFlexible = IsColumnWidthFlexible;
            flowDocument.IsHyphenationEnabled = IsHyphenationEnabled;
            flowDocument.IsOptimalParagraphEnabled = IsOptimalParagraphEnabled;
            flowDocument.LineHeight = random.NextDouble() * 5 + 0.005;
            flowDocument.LineStackingStrategy = random.NextEnum<LineStackingStrategy>();
            double minPageHeight = random.NextDouble() * 100 + 100;
            double maxPageHeight = minPageHeight + random.NextDouble() * 900;
            double minPageWidth = random.NextDouble() * 100 + 100;
            double maxPageWidth = minPageWidth + random.NextDouble() * 900;
            flowDocument.MaxPageHeight = maxPageHeight;
            flowDocument.MaxPageWidth = maxPageWidth;
            flowDocument.MinPageHeight = minPageHeight;
            flowDocument.MinPageWidth = minPageWidth;
            flowDocument.PageHeight = minPageHeight + random.NextDouble() * (maxPageHeight - minPageHeight);
            flowDocument.PageWidth = minPageWidth + random.NextDouble() * (maxPageWidth - minPageWidth);
            flowDocument.PagePadding = Thickness;
            flowDocument.TextAlignment = random.NextEnum<TextAlignment>();
            flowDocument.TextEffects = TextEffectCollection;

            return flowDocument;
        }

        public override bool CanCreate(Type desiredType)
        {
            return desiredType.IsAssignableFrom(typeof(FlowDocument));
        }
    }
}
