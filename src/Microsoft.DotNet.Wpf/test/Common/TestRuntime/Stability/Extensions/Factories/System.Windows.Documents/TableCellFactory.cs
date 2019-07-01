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
    internal class TableCellFactory : TextElementFactory<TableCell>
    {
        #region Public Members

        public List<Block> Blocks { get; set; }

        public Brush BorderBrush { get; set; }

        public Thickness BorderThickness { get; set; }

        public int ColumnSpan { get; set; }

        public FlowDirection FlowDirection { get; set; }

        public double LineHeight { get; set; }

        public LineStackingStrategy LineStackingStrategy { get; set; }

        public Thickness Padding { get; set; }

        public int RowSpan { get; set; }

        public TextAlignment TextAlignment { get; set; }

        #endregion

        #region Override Members

        public override TableCell Create(DeterministicRandom random)
        {
            TableCell tableCell = new TableCell();

            ApplyTextElementFactory(tableCell, random);
            HomelessTestHelpers.Merge(tableCell.Blocks, Blocks);
            tableCell.BorderBrush = BorderBrush;
            tableCell.BorderThickness = BorderThickness;
            //Set ColumnSpan and RowSpan between 1 and 10.
            tableCell.ColumnSpan = ColumnSpan % 10 + 1;
            tableCell.RowSpan = RowSpan % 10 + 1;
            tableCell.FlowDirection = FlowDirection;
            tableCell.LineHeight = CreateValidLineHeight(LineHeight);
            tableCell.LineStackingStrategy = LineStackingStrategy;
            tableCell.Padding = Padding;
            tableCell.TextAlignment = TextAlignment;

            return tableCell;
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
