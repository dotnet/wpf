// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Windows;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    internal abstract class BlockFactory<BlockType> : TextElementFactory<BlockType> where BlockType : Block
    {
        #region Public Members

        public Brush BorderBrush { get; set; }

        public Thickness BorderThickness { get; set; }

        public Thickness Margin { get; set; }

        public Thickness Padding { get; set; }

        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public XmlLanguage Language { get; set; }

        public double LineHeight { get; set; }

        #endregion

        #region Protected Members

        protected void ApplyBlockProperties(BlockType block, DeterministicRandom random)
        {
            ApplyTextElementFactory(block, random);
            block.BorderBrush = BorderBrush;
            block.BorderThickness = BorderThickness;
            //HACK: Work around bug 891857.
            //block.BreakColumnBefore = random.NextBool();
            //HACK: Work around bug 891734.
            //block.BreakPageBefore = random.NextBool();
            block.ClearFloaters = random.NextEnum<WrapDirection>();
            block.FlowDirection = random.NextEnum<FlowDirection>();
            block.IsHyphenationEnabled = random.NextBool();
            block.LineHeight = CreateValidLineHeight(LineHeight);
            block.LineStackingStrategy = random.NextEnum<LineStackingStrategy>();
            //HACK: Work around bug 891876.
            //block.Margin = Margin;
            block.Padding = Padding;
            block.TextAlignment = random.NextEnum<TextAlignment>();
            block.Language = Language;
        }

        #endregion

        #region Private Members

        /// <summary>
        /// Double value need equal to or greater than 0.0034 and equal to or less then 160000, or double.NaN.
        /// </summary>
        private double CreateValidLineHeight(double OriginalLineHeight)
        {
            if (OriginalLineHeight < 0.034)
            {
                return 0.034;
            }

            if (OriginalLineHeight > 200000)
            {
                return double.NaN;
            }

            if (OriginalLineHeight > 160000)
            {
                return 160000;
            }

            return OriginalLineHeight;
        }

        #endregion
    }
}
