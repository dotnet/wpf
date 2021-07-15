// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
//  Contents:  Text source callback methods
//
//  Spec:      Text Formatting API.doc
//
//


using System;


namespace System.Windows.Media.TextFormatting
{
    /// <summary>
    /// The client provides a concrete implementation of the abstract 
    /// TextSource class in order to provide both character data and 
    /// formatting properties to the text formatting engine.
    /// 
    /// All access to text in the TextSource is through the GetTextRun 
    /// method which is designed to allow the client to virtualize text 
    /// in any way it chooses.
    /// </summary>
    public abstract class TextSource 
    {
        /// <summary>
        /// TextFormatter to get a text run started at specified text source position
        /// </summary>
        /// <param name="textSourceCharacterIndex">character index to specify where in the source text the fetch is to start.</param>
        /// <returns>text run corresponding to textSourceCharacterIndex.</returns>
        public abstract TextRun GetTextRun(
            int         textSourceCharacterIndex
            );


        /// <summary>
        /// TextFormatter to get text span immediately before specified text source position.
        /// </summary>
        /// <param name="textSourceCharacterIndexLimit">character index to specify where in the source text the text retrieval stops.</param>
        /// <returns>text span immediately before the specify text source character index.</returns>
        /// <remarks> 
        /// Return empty CharacterBufferRange in the text span if the text span immediately before the 
        /// specified position doesn't contain any text (such as inline object or hidden run). 
        /// Return a zero length TextSpan if there is nothing preceding the specified position.
        /// </remarks>
        public abstract TextSpan<CultureSpecificCharacterBufferRange> GetPrecedingText(
            int         textSourceCharacterIndexLimit
            );


        /// <summary>
        /// TextFormatter to map a text source character index to a text effect character index        
        /// </summary>
        /// <param name="textSourceCharacterIndex"> text source character index </param>
        /// <returns> the text effect index corresponding to the text source character index </returns>
        public abstract int GetTextEffectCharacterIndexFromTextSourceCharacterIndex(
            int         textSourceCharacterIndex
            );

        /// <summary>
        /// PixelsPerDip at which the text should be rendered. Any class which extends TextSource should update
        /// this property whenever DPI changes for a Per Monitor DPI Aware Application.
        /// </summary>
        public double PixelsPerDip
        {
            get { return _pixelsPerDip; }
            set { _pixelsPerDip = value; }
        }

        private double _pixelsPerDip = MS.Internal.FontCache.Util.PixelsPerDip;
    }
}
