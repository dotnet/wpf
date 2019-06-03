// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//
// Description: TextFormatter host.
//


using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;

namespace MS.Internal.PtsHost
{
    // ----------------------------------------------------------------------
    // TextFormatter host.
    // ----------------------------------------------------------------------
    internal sealed class TextFormatterHost : TextSource
    {
        internal TextFormatterHost(TextFormatter textFormatter, TextFormattingMode textFormattingMode, double pixelsPerDip)
        {
            if(textFormatter == null)
            {
                TextFormatter = TextFormatter.FromCurrentDispatcher(textFormattingMode);
            }
            else
            {
                TextFormatter = textFormatter;
            }

            PixelsPerDip = pixelsPerDip;
        }

        //-------------------------------------------------------------------
        // GetTextRun
        //-------------------------------------------------------------------
        public override TextRun GetTextRun(int textSourceCharacterIndex)
        {
            Debug.Assert(Context != null, "TextFormatter host is not initialized.");
            Debug.Assert(textSourceCharacterIndex >= 0, "Character index must be non-negative.");
            TextRun run = Context.GetTextRun(textSourceCharacterIndex);
            if (run.Properties != null)
            {
                run.Properties.PixelsPerDip = PixelsPerDip;
            }

            return run;
        }

        //-------------------------------------------------------------------
        // GetPrecedingText
        //-------------------------------------------------------------------
        public override TextSpan<CultureSpecificCharacterBufferRange> GetPrecedingText(int textSourceCharacterIndexLimit)
        {
            Debug.Assert(Context != null, "TextFormatter host is not initialized.");
            Debug.Assert(textSourceCharacterIndexLimit >= 0, "Character index must be non-negative.");
            return Context.GetPrecedingText(textSourceCharacterIndexLimit);
        }

        /// <summary>
        /// TextFormatter to map a text source character index to a text effect character index        
        /// </summary>
        /// <param name="textSourceCharacterIndex"> text source character index </param>
        /// <returns> the text effect index corresponding to the text effect character index </returns>
        public override int GetTextEffectCharacterIndexFromTextSourceCharacterIndex(
            int textSourceCharacterIndex
            )
        {
            Debug.Assert(Context != null, "TextFormatter host is not initialized.");
            Debug.Assert(textSourceCharacterIndex>= 0, "Character index must be non-negative.");
            return Context.GetTextEffectCharacterIndexFromTextSourceCharacterIndex(textSourceCharacterIndex);
        }
        
        //-------------------------------------------------------------------
        // TextFormatterHost context, object responsible for providing 
        // formatting information.
        //-------------------------------------------------------------------
        internal LineBase Context;

        //-------------------------------------------------------------------
        // TextFormatter.
        //-------------------------------------------------------------------
        internal TextFormatter TextFormatter;
    }
}
