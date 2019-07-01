// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    [TargetTypeAttribute(typeof(TextElement))]
    internal class ListItemFactory : TextElementFactory<ListItem>
    {
        #region Public Members

        public Paragraph Paragraph { get; set; }

        public Brush BorderBrush { get; set; }

        public Thickness BorderThickness { get; set; }

        public Thickness Margin { get; set; }

        public Thickness Padding { get; set; }

        public List<Block> Children { get; set; }

        public double LineHeight { get; set; }

        #endregion

        #region Override Members

        public override ListItem Create(DeterministicRandom random)
        {
            ListItem listItem = null;

            if (Paragraph != null)
            {
                listItem = new ListItem(Paragraph);
            }
            else
            {
                listItem = new ListItem();
            }

            ApplyTextElementFactory(listItem, random);
            HomelessTestHelpers.Merge(listItem.Blocks, Children);
            listItem.BorderBrush = BorderBrush;
            listItem.BorderThickness = BorderThickness;
            listItem.FlowDirection = random.NextEnum<FlowDirection>();
            listItem.LineHeight = CreateValidLineHeight(LineHeight);
            listItem.LineStackingStrategy = random.NextEnum<LineStackingStrategy>();
            listItem.Margin = Margin;
            listItem.Padding = Padding;
            listItem.TextAlignment = random.NextEnum<TextAlignment>();

            return listItem;
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
