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
    internal abstract class AnchoredBlockFactory<AnchoredBlockType> : InlineFactory<AnchoredBlockType> where AnchoredBlockType : AnchoredBlock
    {
        #region Public Members

        public Brush BorderBrush { get; set; }

        public Thickness BorderThickness { get; set; }

        public Thickness Margin { get; set; }

        public Thickness Padding { get; set; }

        public double LineHeight { get; set; }

        public List<Block> Blocks { get; set; }

        #endregion

        #region Protected Members

        protected void ApplyAnchoredBlockProperties(AnchoredBlockType anchoredBlock, DeterministicRandom random)
        {
            ApplyInlineProperties(anchoredBlock, random);
            HomelessTestHelpers.Merge(anchoredBlock.Blocks, Blocks);
            anchoredBlock.BorderBrush = BorderBrush;
            anchoredBlock.BorderThickness = BorderThickness;
            anchoredBlock.LineHeight = CreateValidLineHeight(LineHeight);
            anchoredBlock.LineStackingStrategy = random.NextEnum<LineStackingStrategy>();
            anchoredBlock.Margin = Margin;
            anchoredBlock.Padding = Padding;
            anchoredBlock.TextAlignment = random.NextEnum<TextAlignment>();
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
