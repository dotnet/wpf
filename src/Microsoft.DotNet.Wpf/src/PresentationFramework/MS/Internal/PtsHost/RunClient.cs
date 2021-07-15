// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
// Description: RunClient represents opaque data storage associated with runs
//              consumed by TextFormatter.
//


using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using MS.Internal;
using MS.Internal.Text;
using MS.Internal.Documents;
using MS.Internal.PtsHost.UnsafeNativeMethods;

namespace MS.Internal.PtsHost
{
    /// <summary>
    /// Inline object run.
    /// </summary>
    internal sealed class InlineObjectRun : TextEmbeddedObject
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="cch">Number of text position in the text array occupied by the inline object.</param>
        /// <param name="element">UIElement representing the inline object.</param>
        /// <param name="textProps">Text run properties for the inline object.</param>
        /// <param name="host">Paragraph - the host of the inline object.</param>
        internal InlineObjectRun(int cch, UIElement element, TextRunProperties textProps, TextParagraph host)
        {
            _cch = cch;
            _textProps = textProps;
            _host = host;

            _inlineUIContainer = (InlineUIContainer)LogicalTreeHelper.GetParent(element);
        }

        /// <summary>
        /// Get inline object's measurement metrics.
        /// </summary>
        /// <param name="remainingParagraphWidth">Remaining paragraph width.</param>
        /// <returns>Inline object metrics.</returns>
        public override TextEmbeddedObjectMetrics Format(double remainingParagraphWidth)
        {
            Size desiredSize = _host.MeasureChild(this);

            // Make sure that LS/PTS limitations are not exceeded for object's size.
            TextDpi.EnsureValidObjSize(ref desiredSize);

            double baseline = desiredSize.Height;
            double baselineOffsetValue = (double)UIElementIsland.Root.GetValue(TextBlock.BaselineOffsetProperty);

            if(!DoubleUtil.IsNaN(baselineOffsetValue))
            {
                baseline = baselineOffsetValue;
            }
            return new TextEmbeddedObjectMetrics(desiredSize.Width, desiredSize.Height, baseline);
        }

        /// <summary>
        /// Get computed bounding box of the inline object.
        /// </summary>
        /// <param name="rightToLeft">Run is drawn from right to left.</param>
        /// <param name="sideways">Run is drawn with its side parallel to baseline.</param>
        /// <returns>Computed bounding box size of text object.</returns>
        public override Rect ComputeBoundingBox(bool rightToLeft, bool sideways)
        {
            // Initially assume that bounding box is the same as layout box.
            // NOTE: PTS requires bounding box during line formatting. This eventually triggers
            //       TextFormatter to call this function.
            //       But at that time computed size is not available yet. Use desired size instead.
            Size size = UIElementIsland.Root.DesiredSize;
            double baseline = !sideways ? size.Height : size.Width;
            double baselineOffsetValue = (double)UIElementIsland.Root.GetValue(TextBlock.BaselineOffsetProperty);

            if (!sideways && !DoubleUtil.IsNaN(baselineOffsetValue))
            {
                baseline = (double) baselineOffsetValue;
            }

            return new Rect(0, -baseline, sideways ? size.Height : size.Width, sideways ? size.Width : size.Height);
        }

        /// <summary>
        /// Draw the inline object.
        /// </summary>
        /// <param name="drawingContext">Drawing context.</param>
        /// <param name="origin">Origin where the object is drawn.</param>
        /// <param name="rightToLeft">Run is drawn from right to left.</param>
        /// <param name="sideways">Run is drawn with its side parallel to baseline.</param>
        public override void Draw(DrawingContext drawingContext, Point origin, bool rightToLeft, bool sideways)
        {
            // Inline object has its own visual and it is attached to a visual
            // tree during arrange process.
            // Do nothing here.
        }

        /// <summary>
        /// Reference to character buffer.
        /// </summary>
        public override CharacterBufferReference CharacterBufferReference { get { return new CharacterBufferReference(String.Empty, 0); } }

        /// <summary>
        /// Length of the inline object run.
        /// </summary>
        public override int Length { get { return _cch; } }

        /// <summary>
        /// A set of properties shared by every characters in the run
        /// </summary>
        public override TextRunProperties Properties { get { return _textProps;  } }

        /// <summary>
        /// Line break condition before the inline object.
        /// </summary>
        public override LineBreakCondition BreakBefore
        {
            get
            {
                return LineBreakCondition.BreakDesired;
            }
        }

        /// <summary>
        /// Line break condition after the inline object.
        /// </summary>
        public override LineBreakCondition BreakAfter
        {
            get
            {
                return LineBreakCondition.BreakDesired;
            }
        }

        /// <summary>
        /// Flag indicates whether inline object has fixed size regardless of where
        /// it is placed within a line.
        /// </summary>
        public override bool HasFixedSize
        {
            get
            {
                // Size of inline object is not dependent on position in the line.
                return true;
            }
        }

        /// <summary>
        /// UIElementIsland representing embedded Element Layout island within content world.
        /// </summary>
        internal UIElementIsland UIElementIsland
        {
            get
            {
                return _inlineUIContainer.UIElementIsland;
            }
        }

        /// <summary>
        /// Number of text position in the text array occupied by the inline object.
        /// </summary>
        private readonly int _cch;

        /// <summary>
        /// Text run properties for the inline object.
        /// </summary>
        private readonly TextRunProperties _textProps;

        /// <summary>
        /// Paragraph - the host of the inline object.
        /// </summary>
        private readonly TextParagraph _host;

        /// <summary>
        /// Inline UI Container associated with this run client
        /// </summary>
        private InlineUIContainer _inlineUIContainer;
    }

    /// <summary>
    /// Floating object run.
    /// </summary>
    internal sealed class FloatingRun : TextHidden
    {
        internal FloatingRun(int length, bool figure) : base(length)
        {
            _figure = figure;
        }

        internal bool Figure { get { return _figure; } }

        private readonly bool _figure;
    }

    /// <summary>
    /// Custom paragraph break run.
    /// </summary>
    internal sealed class ParagraphBreakRun : TextEndOfParagraph
    {
        internal ParagraphBreakRun(int length, PTS.FSFLRES breakReason) : base(length)
        {
            BreakReason = breakReason;
        }

        internal readonly PTS.FSFLRES BreakReason;
    }

    /// <summary>
    /// Custom line break run.
    /// </summary>
    internal sealed class LineBreakRun : TextEndOfLine
    {
        internal LineBreakRun(int length, PTS.FSFLRES breakReason) : base(length)
        {
            BreakReason = breakReason;
        }

        internal readonly PTS.FSFLRES BreakReason;
    }
}
