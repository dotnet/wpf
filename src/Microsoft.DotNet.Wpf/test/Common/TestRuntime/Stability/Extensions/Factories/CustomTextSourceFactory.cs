// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
    
using System;
using System.Text;
using System.Windows.Media.TextFormatting;
using Microsoft.Test.Stability.Core;
using Microsoft.Test.Stability.Extensions.Constraints;

namespace Microsoft.Test.Stability.Extensions.Factories
{
    /// <summary>
    /// This Factory is used for creating CustomTextSource objects, for specifying character data and 
    /// formatting properties to be used by the TextFormatter object.
    /// </summary>
    [TargetTypeAttribute(typeof(CustomTextSource))]
    public class CustomTextSourceFactory : DiscoverableFactory<CustomTextSource>
    {
        public CustomTextParagraphProperties CTPProperties { get; set; }

        [InputAttribute(ContentInputSource.CreateFromConstraints)]
        public double ParagraphWidth { get; set; }

        public override CustomTextSource Create(DeterministicRandom random)
        {
            CustomTextSource textStore = new CustomTextSource(CTPProperties);
            textStore.ParagraphWidth = ParagraphWidth * Microsoft.Test.Display.Monitor.Dpi.x;
            return textStore;
        }
    }

    /// <summary>
    /// CustomTextSource is our implementation of TextSource class for specifying character data and 
    /// formatting properties to be used by the TextFormatter object. This implementation is very simplistic.
    /// The entire text content is considered a single span and all changes to the size, alignment, font, etc. 
    /// are applied across the entire text.
    /// </summary>
    public class CustomTextSource : TextSource
    {
        #region Constructor

        public CustomTextSource(CustomTextParagraphProperties CTPProperties)
        {
            this.CTPProperties = CTPProperties;
        }

        #endregion

        #region Public Methods
        // Used by the TextFormatter object to retrieve a run of text from the text source.
        public override TextRun GetTextRun(int textSourceCharacterIndex)
        {
            // Make sure text source index is in bounds.
            if (textSourceCharacterIndex < 0)
                throw new ArgumentOutOfRangeException("textSourceCharacterIndex", "Value must be greater than 0.");
            if (textSourceCharacterIndex >= Text.Length)
            {
                return new TextEndOfParagraph(1);
            }

            // Create TextCharacters using the current font rendering properties.
            if (textSourceCharacterIndex < Text.Length)
            {
                return new TextCharacters(Text, textSourceCharacterIndex,
                                          Text.Length - textSourceCharacterIndex,
                                          CTPProperties.DefaultTextRunProperties);
            }

            // Return an end-of-paragraph if no more text source.
            return new TextEndOfParagraph(1);
        }

        public override TextSpan<CultureSpecificCharacterBufferRange> GetPrecedingText(int textSourceCharacterIndexLimit)
        {
            CharacterBufferRange cbr = new CharacterBufferRange(Text, 0, textSourceCharacterIndexLimit);
            return new TextSpan<CultureSpecificCharacterBufferRange>(textSourceCharacterIndexLimit,
                       new CultureSpecificCharacterBufferRange(System.Globalization.CultureInfo.CurrentUICulture, cbr));
        }

        /// <summary>
        /// This method is not called. It is just there in order to satisfy the condition of extending TextSource abstract class.
        /// </summary>
        public override int GetTextEffectCharacterIndexFromTextSourceCharacterIndex(int textSourceCharacterIndex)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion

        #region Properties

        public string Text { get; set; }

        public double ParagraphWidth  { get; set; }

        public CustomTextParagraphProperties CTPProperties  { get; set; }

        #endregion
    }
}
