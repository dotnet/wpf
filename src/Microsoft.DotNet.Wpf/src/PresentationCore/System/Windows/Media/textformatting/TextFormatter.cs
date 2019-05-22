// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
//  Contents:  Text formatting API
//
//  Spec:      Text Formatting API.doc
//
//


using System;
using System.Windows.Threading;
using System.Collections.Generic;

using MS.Internal;
using MS.Internal.TextFormatting;
using MS.Internal.PresentationCore;


namespace System.Windows.Media.TextFormatting
{
    /// <summary>
    /// The TextFormatter is the Windows Client Platform text engine that provides services 
    /// for formatting text and breaking text lines. It handles different text character 
    /// formats with different paragraph styles. It also implements all the writing system rules
    /// for international text layout.
    /// 
    /// Unlike traditional text APIs, the TextFormatter interacts with a text layout client 
    /// through a set of callback methods. It requires the client to provide these methods 
    /// in an implementation of the TextSource class.
    /// </summary>
    public abstract class TextFormatter : IDisposable
    {
        private static object _staticLock = new object();

        /// <summary>
        /// Client to create a new instance of TextFormatter
        /// </summary>
        /// <returns>New instance of TextFormatter</returns>
        static public TextFormatter Create(TextFormattingMode textFormattingMode)
        {
            if ((int)textFormattingMode < 0 || (int)textFormattingMode > 1)
            {
                throw new System.ComponentModel.InvalidEnumArgumentException("textFormattingMode", (int)textFormattingMode, typeof(TextFormattingMode));
            }
            
            // create a new instance of TextFormatter which allows the use of multiple contexts.
            return new TextFormatterImp(textFormattingMode);
        }

        /// <summary>
        /// Client to create a new instance of TextFormatter
        /// </summary>
        /// <returns>New instance of TextFormatter</returns>
        static public TextFormatter Create()
        {
            // create a new instance of TextFormatter which allows the use of multiple contexts.
            return new TextFormatterImp();
        }


        /// <summary>
        /// Client to create a new instance of TextFormatter from the specified TextFormatter context
        /// </summary>
        /// <param name="soleContext">TextFormatter context</param>
        /// <returns>New instance of TextFormatter</returns>
#if OPTIMALBREAK_API
        static public TextFormatter CreateFromContext(TextFormatterContext soleContext)
#else
        [FriendAccessAllowed]   // used by Framework
        static internal TextFormatter CreateFromContext(TextFormatterContext soleContext)
#endif
        {
            // create a new instance of TextFormatter for the specified context.
            // This creation prohibits the use of multiple contexts within the same TextFormatter via reentrance.
            return new TextFormatterImp(soleContext, TextFormattingMode.Ideal);
        }

        /// <summary>
        /// Client to create a new instance of TextFormatter from the specified TextFormatter context
        /// </summary>
        /// <param name="soleContext">TextFormatter context</param>
        /// <returns>New instance of TextFormatter</returns>
#if OPTIMALBREAK_API
        static public TextFormatter CreateFromContext(TextFormatterContext soleContext, TextFormattingMode textFormattingMode)
#else
        [FriendAccessAllowed]   // used by Framework
        static internal TextFormatter CreateFromContext(TextFormatterContext soleContext, TextFormattingMode textFormattingMode)
#endif
        {
            // create a new instance of TextFormatter for the specified context.
            // This creation prohibits the use of multiple contexts within the same TextFormatter via reentrance.
            return new TextFormatterImp(soleContext, textFormattingMode);
        }


        /// <summary>
        /// Client to get TextFormatter associated with the current dispatcher
        /// </summary>
        /// <remarks>
        /// This method is available internally. It is exclusively used by Presentation Framework UIElement 
        /// through friend assembly mechanics to quickly reuse the default TextFormatter retained in the current
        /// dispatcher of the running thread. 
        /// </remarks>
        [FriendAccessAllowed]   // used by Framework
        static internal TextFormatter FromCurrentDispatcher()
        {
            return FromCurrentDispatcher(TextFormattingMode.Ideal);
        }

        /// <summary>
        /// Client to get TextFormatter associated with the current dispatcher
        /// </summary>
        /// <remarks>
        /// This method is available internally. It is exclusively used by Presentation Framework UIElement 
        /// through friend assembly mechanics to quickly reuse the default TextFormatter retained in the current
        /// dispatcher of the running thread. 
        /// </remarks>
        [FriendAccessAllowed]   // used by Framework
        static internal TextFormatter FromCurrentDispatcher(TextFormattingMode textFormattingMode)
        {
            Dispatcher dispatcher = Dispatcher.CurrentDispatcher;

            if (dispatcher == null)
                throw new ArgumentException(SR.Get(SRID.CurrentDispatcherNotFound));

            TextFormatter defaultTextFormatter;
            if (textFormattingMode == TextFormattingMode.Display)
            {
                defaultTextFormatter = (TextFormatterImp)dispatcher.Reserved4;
            }
            else
            {
                defaultTextFormatter = (TextFormatterImp)dispatcher.Reserved1;
            }
                        
            if (defaultTextFormatter == null)
            {
                lock (_staticLock)
                {
                    if (defaultTextFormatter == null)
                    {
                        // Default formatter has not been created for this dispatcher,
                        // create a new one and stick it to the dispatcher.
                        defaultTextFormatter = Create(textFormattingMode);

                        if (textFormattingMode == TextFormattingMode.Display)
                        {
                            dispatcher.Reserved4 = defaultTextFormatter;
                        }
                        else
                        {
                            dispatcher.Reserved1 = defaultTextFormatter;
                        }
                    }
                }
            }

            Invariant.Assert(defaultTextFormatter != null);
            return defaultTextFormatter;
        }

        /// <summary>
        /// Clean up text formatter internal resource
        /// </summary>
        public virtual void Dispose() {}



        /// <summary>
        /// Client to format a text line that fills a paragraph in the document.
        /// </summary>
        /// <param name="textSource">an object representing text layout clients text source for TextFormatter.</param>
        /// <param name="firstCharIndex">character index to specify where in the source text the line starts</param>
        /// <param name="paragraphWidth">width of paragraph in which the line fills</param>
        /// <param name="paragraphProperties">properties that can change from one paragraph to the next, such as text flow direction, text alignment, or indentation.</param>
        /// <param name="previousLineBreak">text formatting state at the point where the previous line in the paragraph
        /// was broken by the text formatting process, as specified by the TextLine.LineBreak property for the previous
        /// line; this parameter can be null, and will always be null for the first line in a paragraph.</param>
        /// <returns>object representing a line of text that client interacts with. </returns>
        public abstract TextLine FormatLine(
            TextSource                  textSource,
            int                         firstCharIndex,
            double                      paragraphWidth,
            TextParagraphProperties     paragraphProperties,
            TextLineBreak               previousLineBreak
            );



        /// <summary>
        /// Client to format a text line that fills a paragraph in the document.
        /// </summary>
        /// <param name="textSource">an object representing text layout clients text source for TextFormatter.</param>
        /// <param name="firstCharIndex">character index to specify where in the source text the line starts</param>
        /// <param name="paragraphWidth">width of paragraph in which the line fills</param>
        /// <param name="paragraphProperties">properties that can change from one paragraph to the next, such as text flow direction, text alignment, or indentation.</param>
        /// <param name="previousLineBreak">text formatting state at the point where the previous line in the paragraph
        /// was broken by the text formatting process, as specified by the TextLine.LineBreak property for the previous
        /// line; this parameter can be null, and will always be null for the first line in a paragraph.</param>
        /// <param name="textRunCache">an object representing content cache of the client.</param>
        /// <returns>object representing a line of text that client interacts with. </returns>
        public abstract TextLine FormatLine(
            TextSource                  textSource,
            int                         firstCharIndex,
            double                      paragraphWidth,
            TextParagraphProperties     paragraphProperties,
            TextLineBreak               previousLineBreak,
            TextRunCache                textRunCache
            );



        /// <summary>
        /// Client to reconstruct a previously formatted text line
        /// </summary>
        /// <param name="textSource">an object representing text layout clients text source for TextFormatter.</param>
        /// <param name="firstCharIndex">character index to specify where in the source text the line starts</param>
        /// <param name="lineLength">character length of the line</param>
        /// <param name="paragraphWidth">width of paragraph in which the line fills</param>
        /// <param name="paragraphProperties">properties that can change from one paragraph to the next, such as text flow direction, text alignment, or indentation.</param>
        /// <param name="previousLineBreak">LineBreak property of the previous text line, or null if this is the first line in the paragraph</param>
        /// <param name="textRunCache">an object representing content cache of the client.</param>
        /// <returns>object representing a line of text that client interacts with. </returns>
#if OPTIMALBREAK_API
        public abstract TextLine RecreateLine(
#else
        [FriendAccessAllowed]   // used by Framework
        internal abstract TextLine RecreateLine(
#endif
            TextSource                  textSource,
            int                         firstCharIndex,
            int                         lineLength,
            double                      paragraphWidth,
            TextParagraphProperties     paragraphProperties,
            TextLineBreak               previousLineBreak,
            TextRunCache                textRunCache
            );



        /// <summary>
        /// Client to cache information about a paragraph to be used during optimal paragraph line formatting
        /// </summary>
        /// <param name="textSource">an object representing text layout clients text source for TextFormatter.</param>
        /// <param name="firstCharIndex">character index to specify where in the source text the line starts</param>
        /// <param name="paragraphWidth">width of paragraph in which the line fills</param>
        /// <param name="paragraphProperties">properties that can change from one paragraph to the next, such as text flow direction, text alignment, or indentation.</param>
        /// <param name="previousLineBreak">text formatting state at the point where the previous line in the paragraph
        /// was broken by the text formatting process, as specified by the TextLine.LineBreak property for the previous
        /// line; this parameter can be null, and will always be null for the first line in a paragraph.</param>
        /// <param name="textRunCache">an object representing content cache of the client.</param>
        /// <returns>object representing a line of text that client interacts with. </returns>
#if OPTIMALBREAK_API
        public abstract TextParagraphCache CreateParagraphCache(
#else
        [FriendAccessAllowed]   // used by Framework
        internal abstract TextParagraphCache CreateParagraphCache(
#endif
            TextSource                  textSource,
            int                         firstCharIndex,
            double                      paragraphWidth,
            TextParagraphProperties     paragraphProperties,
            TextLineBreak               previousLineBreak,
            TextRunCache                textRunCache
            );



        /// <summary>
        /// Client to ask for the possible smallest and largest paragraph width that can fully contain the passing text content
        /// </summary>
        /// <param name="textSource">an object representing text layout clients text source for TextFormatter.</param>
        /// <param name="firstCharIndex">character index to specify where in the source text the line starts</param>
        /// <param name="paragraphProperties">properties that can change from one paragraph to the next, such as text flow direction, text alignment, or indentation.</param>
        /// <returns>min max paragraph width</returns>
        public abstract MinMaxParagraphWidth FormatMinMaxParagraphWidth(
            TextSource                  textSource,
            int                         firstCharIndex,
            TextParagraphProperties     paragraphProperties
            );



        /// <summary>
        /// Client to ask for the possible smallest and largest paragraph width that can fully contain the passing text content
        /// </summary>
        /// <param name="textSource">an object representing text layout clients text source for TextFormatter.</param>
        /// <param name="firstCharIndex">character index to specify where in the source text the line starts</param>
        /// <param name="paragraphProperties">properties that can change from one paragraph to the next, such as text flow direction, text alignment, or indentation.</param>
        /// <param name="textRunCache">an object representing content cache of the client.</param>
        /// <returns>min max paragraph width</returns>
        public abstract MinMaxParagraphWidth FormatMinMaxParagraphWidth(
            TextSource                  textSource,
            int                         firstCharIndex,
            TextParagraphProperties     paragraphProperties,
            TextRunCache                textRunCache
            );
    }
}
