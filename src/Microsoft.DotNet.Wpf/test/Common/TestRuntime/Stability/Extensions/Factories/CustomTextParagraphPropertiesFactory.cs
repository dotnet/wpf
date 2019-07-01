// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System.Windows;
using System.Windows.Media.TextFormatting;
using Microsoft.Test.Stability.Core;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    class CustomTextParagraphPropertiesFactory : DiscoverableFactory<CustomTextParagraphProperties>
    {
        private const double MaxLineIndent = 1; // 1" maximum line indent
        private const double MaxParagraphIndent = 1.5; // 1.5" maximum paragraph indent

        /// <summary>
        /// The TextRunProperties which provides a set of properties, such as typeface or foreground brush, 
        /// that can be applied to a TextRun object.
        /// </summary>
        public CustomTextRunProperties TextRunProperties { get; set; }

        /// <summary>
        /// The constructor which randomly selects FlowDirection, TextAlignment, 
        /// firstLineInParagraph, alwaysCollapsible, and TextWrapping.
        /// </summary>
        public override CustomTextParagraphProperties Create(DeterministicRandom random)
        {
            FlowDirection flowDirection = random.NextEnum<FlowDirection>();
            TextAlignment alignment = random.NextEnum<TextAlignment>();
            bool firstLineInParagraph = random.NextBool();
            bool alwaysCollapsible = random.NextBool();
            TextWrapping textWrap = random.NextEnum<TextWrapping>();
            double lineHeight = 0; // Set to zero in order to automatically compute the appropriate line height size.
            double indent = random.NextDouble() * MaxLineIndent * Microsoft.Test.Display.Monitor.Dpi.x;
            double paragraphIndent = random.NextDouble() * MaxParagraphIndent * Microsoft.Test.Display.Monitor.Dpi.x;

            CustomTextParagraphProperties customTextParagraphProperties = new CustomTextParagraphProperties(flowDirection, alignment, firstLineInParagraph,
                                                                                                            alwaysCollapsible, TextRunProperties,
                                                                                                            textWrap, lineHeight, indent, paragraphIndent);

            return customTextParagraphProperties;
        }
    }

    /// <summary>
    /// Class to implement TextParagraphProperties which provides a set of properties, such as flow direction, 
    /// alignment, or indentation, that can be applied to a paragraph (used by TextSource).
    /// </summary>
    public class CustomTextParagraphProperties : TextParagraphProperties
    {
        #region Constructor

        public CustomTextParagraphProperties(FlowDirection flowDirection, TextAlignment textAlignment, bool firstLineInParagraph,
                                              bool alwaysCollapsible, TextRunProperties defaultTextRunProperties,
                                              TextWrapping textWrap, double lineHeight, double indent, double paragraphIndent)
        {
            this.flowDirection = flowDirection;
            this.textAlignment = textAlignment;
            this.firstLineInParagraph = firstLineInParagraph;
            this.alwaysCollapsible = alwaysCollapsible;
            this.defaultTextRunProperties = defaultTextRunProperties;
            this.textWrap = textWrap;
            this.lineHeight = lineHeight;
            this.indent = indent;
            this.paragraphIndent = paragraphIndent;
        }

        #endregion

        #region Properties

        public override FlowDirection FlowDirection
        {
            get { return flowDirection; }
        }

        public override TextAlignment TextAlignment
        {
            get { return textAlignment; }
        }

        public override bool FirstLineInParagraph
        {
            get { return firstLineInParagraph; }
        }

        public override bool AlwaysCollapsible
        {
            get { return alwaysCollapsible; }
        }

        public override TextRunProperties DefaultTextRunProperties
        {
            get { return defaultTextRunProperties; }
        }

        public override TextWrapping TextWrapping
        {
            get { return textWrap; }
        }

        public override double LineHeight
        {
            get { return lineHeight; }
        }

        public override double Indent
        {
            get { return indent; }
        }

        public override TextMarkerProperties TextMarkerProperties
        {
            get { return null; }
        }

        public override double ParagraphIndent
        {
            get { return paragraphIndent; }
        }
        
        #endregion

        #region Private Fields

        private FlowDirection flowDirection;
        private TextAlignment textAlignment;
        private bool firstLineInParagraph;
        private bool alwaysCollapsible;
        private TextRunProperties defaultTextRunProperties;
        private TextWrapping textWrap;
        private double indent;
        private double paragraphIndent;
        private double lineHeight;

        #endregion
    }
}
