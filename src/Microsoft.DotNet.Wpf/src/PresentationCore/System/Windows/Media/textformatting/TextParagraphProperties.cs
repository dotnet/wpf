// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
//  Contents:  Text paragraph properties
//
//  Spec:      Text Formatting API.doc
//
//


using System;
using System.Collections.Generic;
using System.Windows;
using MS.Internal.PresentationCore;


namespace System.Windows.Media.TextFormatting
{
    /// <summary>
    /// Properties that can change from one paragraph to the next, such as 
    /// text flow direction, text alignment, or indentation.
    /// </summary>
    public abstract class TextParagraphProperties
    {
        /// <summary>
        /// This property specifies whether the primary text advance 
        /// direction shall be left-to-right, right-to-left, or top-to-bottom.
        /// </summary>
        public abstract FlowDirection FlowDirection
        { get; }


        /// <summary>
        /// This property describes how inline content of a block is aligned.
        /// </summary>
        public abstract TextAlignment TextAlignment
        { get; }


        /// <summary>
        /// Paragraph's line height
        /// </summary>
        public abstract double LineHeight
        { get; }


        /// <summary>
        /// Indicates the first line of the paragraph.
        /// </summary>
        public abstract bool FirstLineInParagraph
        { get; }


        /// <summary>
        /// If true, the formatted line may always be collapsed. If false (the default),
        /// only lines that overflow the paragraph width are collapsed.
        /// </summary>
        public virtual bool AlwaysCollapsible
        {
            get { return false; }
        }


        /// <summary>
        /// Paragraph's default run properties
        /// </summary>
        public abstract TextRunProperties DefaultTextRunProperties
        { get; }


        /// <summary>
        /// If not null, text decorations to apply to all runs in the line. This is in addition
        /// to any text decorations specified by the TextRunProperties for individual text runs.
        /// </summary>
        public virtual TextDecorationCollection TextDecorations
        {
            get { return null; }
        }


        /// <summary>
        /// This property controls whether or not text wraps when it reaches the flow edge 
        /// of its containing block box 
        /// </summary>
        public abstract TextWrapping TextWrapping
        { get; }


        /// <summary>
        /// This property specifies marker characteristics of the first line in paragraph
        /// </summary>
        public abstract TextMarkerProperties TextMarkerProperties
        { get; }


        /// <summary>
        /// Line indentation
        /// </summary>
        public abstract double Indent
        { get; }


        /// <summary>
        /// Paragraph indentation
        /// </summary>
        public virtual double ParagraphIndent
        {
            get { return 0; }
        }


        /// <summary>
        /// Default Incremental Tab
        /// </summary>
        public virtual double DefaultIncrementalTab
        {
            get { return 4 * DefaultTextRunProperties.FontRenderingEmSize; }
        }


        /// <summary>
        /// Collection of tab definitions
        /// </summary>
        public virtual IList<TextTabProperties> Tabs
        {
            get { return null; }
        }


        /// <summary>
        /// Lexical component providing hyphenation opportunity.
        /// </summary>
#if HYPHENATION_API
        public virtual TextLexicalService Hyphenator
        {
            get { return null; }
        }
#else
        private TextLexicalService  _hyphenator;
        internal virtual TextLexicalService Hyphenator
        {
            [FriendAccessAllowed]   // used by Framework
            get { return _hyphenator; }

            [FriendAccessAllowed]   // used by Framework
            set { _hyphenator = value; }
        }
#endif
    }
}

