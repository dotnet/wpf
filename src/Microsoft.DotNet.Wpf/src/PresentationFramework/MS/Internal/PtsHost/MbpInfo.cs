// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
// Description: Provides paragraph level margin collapsing support. 
//

using System;
using System.Windows;                       // DependencyObject
using System.Windows.Documents;             // Block
using MS.Internal.Text;                     // TextDpi
using System.Windows.Media;                 // Brush
using MS.Internal.PtsHost.UnsafeNativeMethods;  // Pts

namespace MS.Internal.PtsHost
{
    /// <summary>
    /// Provides services for MBP handling.
    /// </summary>
    internal sealed class MbpInfo
    {
        /// <summary>
        /// Get MbpInfo from DependencyObject.
        /// </summary>
        /// <param name="o">DependencyObject for which MBP properties are retrieved.</param>
        internal static MbpInfo FromElement(DependencyObject o, double pixelsPerDip)
        {
            if (o is Block || o is AnchoredBlock || o is TableCell || o is ListItem)
            {
                MbpInfo mbp = new MbpInfo((TextElement)o);
                double lineHeight = DynamicPropertyReader.GetLineHeightValue(o);

                if (mbp.IsMarginAuto)
                {
                    ResolveAutoMargin(mbp, o, lineHeight);
                }
                if (mbp.IsPaddingAuto)
                {
                    ResolveAutoPadding(mbp, o, lineHeight, pixelsPerDip);
                }

                return mbp;
            }

            return _empty;
        }


        /// <summary>
        /// Mirrors margin
        /// </summary>
        internal void MirrorMargin()
        {
            ReverseFlowDirection(ref _margin);
        }

        /// <summary>
        /// Mirrors border and padding
        /// </summary>
        internal void MirrorBP()
        {
            ReverseFlowDirection(ref _border);
            ReverseFlowDirection(ref _padding);
        }

        /// <summary>
        /// Reverses flow direction for a given thickness
        /// </summary>
        private static void ReverseFlowDirection(ref Thickness thickness)
        {
            double temp = thickness.Left;
            thickness.Left = thickness.Right;
            thickness.Right = temp;
        }

        /// <summary>
        /// Static constructor.
        /// </summary>
        static MbpInfo()
        {
            _empty = new MbpInfo();
        }

        /// <summary>
        /// Private constructor.
        /// </summary>
        private MbpInfo()
        {
            _margin = new Thickness();
            _border = new Thickness();
            _padding = new Thickness();
            _borderBrush = new SolidColorBrush();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="block">Block for which MBP properties are retrieved.</param>
        private MbpInfo(TextElement block)
        {
            _margin  = (Thickness)block.GetValue(Block.MarginProperty);
            _border  = (Thickness)block.GetValue(Block.BorderThicknessProperty);
            _padding = (Thickness)block.GetValue(Block.PaddingProperty);
            _borderBrush = (Brush)block.GetValue(Block.BorderBrushProperty);
        }

        /// <summary>
        /// Resolve Auto values for Margin.
        /// </summary>
        private static void ResolveAutoMargin(MbpInfo mbp, DependencyObject o, double lineHeight)
        {
            Thickness defaultMargin;
            if (o is Paragraph)
            {
                DependencyObject parent = ((Paragraph)o).Parent;
                if (parent is ListItem || parent is TableCell || parent is AnchoredBlock)
                {
                    defaultMargin = new Thickness(0);
                }
                else
                {
                    defaultMargin = new Thickness(0, lineHeight, 0, lineHeight);
                }
            }
            else if (o is Table || o is List)
            {
                defaultMargin = new Thickness(0, lineHeight, 0, lineHeight);
            }
            else if (o is Figure || o is Floater)
            {
                defaultMargin = new Thickness(0.5 * lineHeight);
            }
            else
            {
                defaultMargin = new Thickness(0);
            }
            mbp.Margin = new Thickness(
                Double.IsNaN(mbp.Margin.Left) ? defaultMargin.Left : mbp.Margin.Left,
                Double.IsNaN(mbp.Margin.Top) ? defaultMargin.Top : mbp.Margin.Top,
                Double.IsNaN(mbp.Margin.Right) ? defaultMargin.Right : mbp.Margin.Right,
                Double.IsNaN(mbp.Margin.Bottom) ? defaultMargin.Bottom : mbp.Margin.Bottom);
        }

        /// <summary>
        /// Resolve Auto values for Padding.
        /// </summary>
        private static void ResolveAutoPadding(MbpInfo mbp, DependencyObject o, double lineHeight, double pixelsPerDip)
        {
            Thickness defaultPadding;
            if (o is Figure || o is Floater)
            {
                defaultPadding = new Thickness(0.5 * lineHeight);
            }
            else if (o is List)
            {
                defaultPadding = ListMarkerSourceInfo.CalculatePadding((List)o, lineHeight, pixelsPerDip);
            }
            else
            {
                defaultPadding = new Thickness(0);
            }
            mbp.Padding = new Thickness(
                Double.IsNaN(mbp.Padding.Left) ? defaultPadding.Left : mbp.Padding.Left,
                Double.IsNaN(mbp.Padding.Top) ? defaultPadding.Top : mbp.Padding.Top,
                Double.IsNaN(mbp.Padding.Right) ? defaultPadding.Right : mbp.Padding.Right,
                Double.IsNaN(mbp.Padding.Bottom) ? defaultPadding.Bottom : mbp.Padding.Bottom);
        }

        /// <value>
        /// Combined value of left Margin, Border and Padding.
        /// </value>
        internal int MBPLeft
        {
            get { return TextDpi.ToTextDpi(_margin.Left) + TextDpi.ToTextDpi(_border.Left) + TextDpi.ToTextDpi(_padding.Left); }
        }

        /// <value>
        /// Combined value of right Margin, Border and Padding.
        /// </value>
        internal int MBPRight
        {
            get { return TextDpi.ToTextDpi(_margin.Right) + TextDpi.ToTextDpi(_border.Right) + TextDpi.ToTextDpi(_padding.Right); }
        }

        /// <value>
        /// Combined value of top Margin, Border and Padding.
        /// </value>
        internal int MBPTop
        {
            get { return TextDpi.ToTextDpi(_margin.Top) + TextDpi.ToTextDpi(_border.Top) + TextDpi.ToTextDpi(_padding.Top); }
        }

        /// <value>
        /// Combined value of top Margin, Border and Padding.
        /// </value>
        internal int MBPBottom
        {
            get { return TextDpi.ToTextDpi(_margin.Bottom) + TextDpi.ToTextDpi(_border.Bottom) + TextDpi.ToTextDpi(_padding.Bottom); }
        }

        /// <value>
        /// Combined value of left Border and Padding.
        /// </value>
        internal int BPLeft
        {
            get { return TextDpi.ToTextDpi(_border.Left) + TextDpi.ToTextDpi(_padding.Left); }
        }

        /// <value>
        /// Combined value of right Border and Padding.
        /// </value>
        internal int BPRight
        {
            get { return TextDpi.ToTextDpi(_border.Right) + TextDpi.ToTextDpi(_padding.Right); }
        }

        /// <value>
        /// Combined value of top Border and Padding.
        /// </value>
        internal int BPTop
        {
            get { return TextDpi.ToTextDpi(_border.Top) + TextDpi.ToTextDpi(_padding.Top); }
        }

        /// <value>
        /// Combined value of bottom Border and Padding.
        /// </value>
        internal int BPBottom
        {
            get { return TextDpi.ToTextDpi(_border.Bottom) + TextDpi.ToTextDpi(_padding.Bottom); }
        }

        /// <value>
        /// Left Border
        /// </value>
        internal int BorderLeft
        {
            get { return TextDpi.ToTextDpi(_border.Left); }
        }

        /// <value>
        /// Right Border
        /// </value>
        internal int BorderRight
        {
            get { return TextDpi.ToTextDpi(_border.Right); }
        }

        /// <value>
        /// Top Border
        /// </value>
        internal int BorderTop
        {
            get { return TextDpi.ToTextDpi(_border.Top); }
        }

        /// <value>
        /// Bottom Border
        /// </value>
        internal int BorderBottom
        {
            get { return TextDpi.ToTextDpi(_border.Bottom); }
        }

        /// <value>
        /// Left margin.
        /// </value>
        internal int MarginLeft
        {
            get { return TextDpi.ToTextDpi(_margin.Left); }
        }

        /// <value>
        /// Right margin.
        /// </value>
        internal int MarginRight
        {
            get { return TextDpi.ToTextDpi(_margin.Right); }
        }

        /// <value>
        /// top  margin.
        /// </value>
        internal int MarginTop
        {
            get { return TextDpi.ToTextDpi(_margin.Top); }
        }

        /// <value>
        /// Bottom margin.
        /// </value>
        internal int MarginBottom
        {
            get { return TextDpi.ToTextDpi(_margin.Bottom); }
        }

        /// <value>
        /// Margin thickness.
        /// </value>
        internal Thickness Margin
        {
            get { return _margin; }
            set { _margin = value; }
        }

        /// <value>
        /// Border thickness.
        /// </value>
        internal Thickness Border
        {
            get { return _border; }
            set { _border = value; }
        }

        internal Thickness Padding
        {
            get { return _padding; }
            set { _padding = value; }
        }

        /// <value>
        /// Border brush.
        /// </value>
        internal Brush BorderBrush
        {
            get { return _borderBrush; }
        }

        /// <summary>
        /// Whether any padding value is Auto.
        /// </summary>
        private bool IsPaddingAuto
        {
            get
            {
                return (
                    Double.IsNaN(_padding.Left) ||
                    Double.IsNaN(_padding.Right) ||
                    Double.IsNaN(_padding.Top) ||
                    Double.IsNaN(_padding.Bottom));
            }
        }

        /// <summary>
        /// Whether any margin value is Auto.
        /// </summary>
        private bool IsMarginAuto
        {
            get
            {
                return (
                    Double.IsNaN(_margin.Left) ||
                    Double.IsNaN(_margin.Right) ||
                    Double.IsNaN(_margin.Top) ||
                    Double.IsNaN(_margin.Bottom));
            }
        }

        /// <value>
        /// Margin thickness.
        /// </value>
        private Thickness _margin;

        /// <value>
        /// Border thickness.
        /// </value>
        private Thickness _border;

        /// <value>
        /// Padding thickness.
        /// </value>
        private Thickness _padding;

        /// <value>
        /// Border brush.
        /// </value>
        private Brush _borderBrush;

        /// <value>
        /// Empty MBPInfo instance.
        /// </value>
        private static MbpInfo _empty;
    }
}

